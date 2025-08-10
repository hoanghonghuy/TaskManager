using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManager.Data.Models
{
    public class Task
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public string? Description { get; set; } // Dấu ? cho phép giá trị null

        public DateTime? DueDate { get; set; } // Có thể không có ngày hết hạn

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Trạng thái mặc định

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Khóa ngoại liên kết với User
        [ForeignKey("User")]
        public int UserId { get; set; }

        // Navigation property
        public User User { get; set; } = null!;
    }
}