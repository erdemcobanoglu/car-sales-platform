namespace Presentation.BackgroundJobs
{
    using Microsoft.EntityFrameworkCore;
    using Presentation.BackgroundJobs.Interfaces;
    using Presentation.BackgroundJobs.PhotoUploads;
    using Presentation.Data;
    using Presentation.Models;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.Formats.Jpeg;
    using SixLabors.ImageSharp.Processing;

    public class PhotoProcessingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPhotoJobQueue _queue;
        private readonly ILogger<PhotoProcessingWorker> _logger;

        public PhotoProcessingWorker(IServiceScopeFactory scopeFactory, IPhotoJobQueue queue, ILogger<PhotoProcessingWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _queue = queue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Guid jobId;
                try
                {
                    jobId = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                try
                {
                    await ProcessJob(jobId, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Photo job failed: {JobId}", jobId);
                    // DB’ye Failed basmayı burada da yapabiliriz (ProcessJob içinde de var)
                }
            }
        }

        private async Task ProcessJob(Guid jobId, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var job = await db.Set<PhotoUploadJob>()
                .Include(j => j.Items)
                .FirstOrDefaultAsync(j => j.Id == jobId, ct);

            if (job == null) return;

            job.Status = UploadJobStatus.Processing;
            await db.SaveChangesAsync(ct);

            try
            {
                var vehicle = await db.Vehicles
                    .FirstOrDefaultAsync(v => v.Id == job.VehicleId && v.OwnerId == job.OwnerId, ct);

                if (vehicle == null) throw new Exception("Vehicle not found or access denied.");

                await db.Entry(vehicle).Collection(v => v.Photos).LoadAsync(ct);

                var uploadRoot = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", "uploads", "vehicles", job.VehicleId.ToString());
                Directory.CreateDirectory(uploadRoot);

                // overflow sil (mevcut + job item)
                var currentPhotos = vehicle.Photos.OrderBy(p => p.SortOrder).ToList();
                var validItems = job.Items.Where(i => System.IO.File.Exists(i.TempPath)).ToList();

                var totalAfter = currentPhotos.Count + validItems.Count;
                var overflow = totalAfter - 10;

                if (overflow > 0)
                {
                    var toDelete = currentPhotos.Take(overflow).ToList();
                    foreach (var p in toDelete)
                    {
                        DeleteImageVariantsFromDisk(p.Url);
                        db.VehiclePhotos.Remove(p);
                        vehicle.Photos.Remove(p);
                    }
                    await db.SaveChangesAsync(ct);
                }

                var nextSort = vehicle.Photos.Any()
                    ? vehicle.Photos.Max(p => p.SortOrder) + 1
                    : 0;

                foreach (var item in validItems)
                {
                    // ImageSharp işlemleri
                    var baseName = $"{Guid.NewGuid():N}";
                    var largeFileName = $"{baseName}_large.jpg";
                    var mediumFileName = $"{baseName}_medium.jpg";
                    var thumbFileName = $"{baseName}_thumb.jpg";

                    var largePath = Path.Combine(uploadRoot, largeFileName);
                    var mediumPath = Path.Combine(uploadRoot, mediumFileName);
                    var thumbPath = Path.Combine(uploadRoot, thumbFileName);

                    try
                    {
                        await using var input = System.IO.File.OpenRead(item.TempPath);
                        using var img = await Image.LoadAsync(input, ct);

                        img.Mutate(x => x.AutoOrient());

                        using (var large = img.Clone(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(1600, 1600)
                        })))
                        {
                            await large.SaveAsJpegAsync(largePath, new JpegEncoder { Quality = 85 }, ct);
                        }

                        using (var medium = img.Clone(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(900, 900)
                        })))
                        {
                            await medium.SaveAsJpegAsync(mediumPath, new JpegEncoder { Quality = 80 }, ct);
                        }

                        using (var thumb = img.Clone(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Crop,
                            Size = new Size(400, 300)
                        })))
                        {
                            await thumb.SaveAsJpegAsync(thumbPath, new JpegEncoder { Quality = 75 }, ct);
                        }
                    }
                    catch
                    {
                        SafeDeleteFile(largePath);
                        SafeDeleteFile(mediumPath);
                        SafeDeleteFile(thumbPath);
                        continue;
                    }
                    finally
                    {
                        SafeDeleteFile(item.TempPath);
                    }

                    var urlLarge = $"/uploads/vehicles/{job.VehicleId}/{largeFileName}";

                    var photo = new VehiclePhoto
                    {
                        VehicleId = job.VehicleId,
                        Url = urlLarge,
                        SortOrder = nextSort++,
                        IsCover = false
                    };

                    db.VehiclePhotos.Add(photo);
                    vehicle.Photos.Add(photo);
                }

                // cover garanti
                if (!vehicle.Photos.Any(p => p.IsCover) && vehicle.Photos.Any())
                {
                    var first = vehicle.Photos.OrderBy(p => p.SortOrder).First();
                    first.IsCover = true;
                }

                await db.SaveChangesAsync(ct);

                job.Status = UploadJobStatus.Completed;
                job.CompletedUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);

                // job temp klasörünü temizle (boş kaldıysa)
                TryDeleteDirectory(Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "temp_uploads", job.Id.ToString("N")));
            }
            catch (Exception ex)
            {
                job.Status = UploadJobStatus.Failed;
                job.Error = ex.Message;
                await db.SaveChangesAsync(ct);
                throw;
            }

            void DeleteImageVariantsFromDisk(string? urlLarge)
            {
                if (string.IsNullOrWhiteSpace(urlLarge)) return;

                var relative = urlLarge.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var largePhysical = Path.Combine(wwwroot, relative);

                SafeDeleteFile(largePhysical);
                SafeDeleteFile(largePhysical.Replace("_large.jpg", "_medium.jpg"));
                SafeDeleteFile(largePhysical.Replace("_large.jpg", "_thumb.jpg"));
            }

            void SafeDeleteFile(string path)
            {
                try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); } catch { }
            }

            void TryDeleteDirectory(string path)
            {
                try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch { }
            }
        }
    }

}
