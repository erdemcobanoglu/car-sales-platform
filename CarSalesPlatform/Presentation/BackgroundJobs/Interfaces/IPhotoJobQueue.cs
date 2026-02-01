namespace Presentation.BackgroundJobs.Interfaces
{
    public interface IPhotoJobQueue
    {
        ValueTask EnqueueAsync(Guid jobId, CancellationToken ct = default);
        ValueTask<Guid> DequeueAsync(CancellationToken ct);
    }
}
