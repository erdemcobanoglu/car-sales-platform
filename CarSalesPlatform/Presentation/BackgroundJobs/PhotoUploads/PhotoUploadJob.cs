namespace Presentation.BackgroundJobs.PhotoUploads
{
    public class PhotoUploadJob
    {
        public Guid Id { get; set; }
        public int VehicleId { get; set; }
        public string OwnerId { get; set; } = default!;
        public UploadJobStatus Status { get; set; }
        public string? Error { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedUtc { get; set; }

        public virtual List<PhotoUploadItem> Items { get; set; } = new();
    }
}
