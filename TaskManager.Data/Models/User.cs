using System.ComponentModel.DataAnnotations;

namespace TaskManager.Data.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property để liên kết với Tasks
        public ICollection<Task> Tasks { get; set; } = new List<Task>();
    }
}