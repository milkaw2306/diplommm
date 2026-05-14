namespace Diploma.Models
{
    public class ExportHistory
    {
        public int ExportId { get; set; }
        public int UserId { get; set; }
        public string? ExportPath { get; set; }
        public int PhotosCount { get; set; }
        public long TotalSize { get; set; }
        public string? ExportType { get; set; } = "original";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}