using System;

namespace Diploma.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? FullName { get; set; }
        public byte[]? Avatar { get; set; }
        public long StorageLimit { get; set; } = 10737418240; // 10 GB
        public long StorageUsed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }
        public string? ResetCode { get; set; }
        public DateTime? ResetCodeExpiry { get; set; }
        public DateTime? LastResetRequest { get; set; } // 👈 ДОБАВИТЬ это поле
        public bool IsActive { get; set; } = true;
    }
}