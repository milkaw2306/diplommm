namespace Diploma.Models
{
    public class ImportHistory
    {
        public int ImportId { get; set; }
        public int UserId { get; set; }
        public string? ImportPath { get; set; }
        public int PhotosCount { get; set; }
        public long TotalSize { get; set; }
        public string? Status { get; set; } = "completed";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
    }
}