using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace TaskManager.Data.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [MaxLength(100)]
        public string? FullName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Task> Tasks { get; set; } = new List<Task>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? PasswordResetAt { get; set; }
    }
}