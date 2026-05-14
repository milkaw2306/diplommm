namespace Diploma.Models
{
    public class ShareLink
    {
        public int ShareId { get; set; }
        public int? FolderId { get; set; }
        public int? PhotoId { get; set; }
        public string? ShareCode { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}