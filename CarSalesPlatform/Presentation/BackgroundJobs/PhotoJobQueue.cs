using Presentation.BackgroundJobs.Interfaces;
using System.Threading.Channels;

namespace Presentation.BackgroundJobs
{
    public class PhotoJobQueue : IPhotoJobQueue
    {
        private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

        public ValueTask EnqueueAsync(Guid jobId, CancellationToken ct = default)
            => _channel.Writer.WriteAsync(jobId, ct);

        public ValueTask<Guid> DequeueAsync(CancellationToken ct)
            => _channel.Reader.ReadAsync(ct);
    }
}
