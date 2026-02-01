namespace Presentation.BackgroundJobs.PhotoUploads
{
    public class PhotoUploadItem
    {
        public int Id { get; set; }
        public Guid JobId { get; set; }
        public virtual PhotoUploadJob Job { get; set; } = null!;

        public string TempPath { get; set; } = default!;
        public string? ContentType { get; set; }
        public long Length { get; set; }
        public string? OriginalFileName { get; set; }
    }
}
