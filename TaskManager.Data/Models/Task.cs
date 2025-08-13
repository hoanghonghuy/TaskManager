using System;
using System.Collections.Generic;
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
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [Required]
        public string Priority { get; set; } = "None";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Thuộc tính cho chức năng Công việc lặp lại
        public string? RecurrenceRule { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        // Thuộc tính cho chức năng Công việc con (Subtasks)
        public int? ParentTaskId { get; set; }
        public Task? ParentTask { get; set; }
        public ICollection<Task> Subtasks { get; set; } = new List<Task>();

        public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    }
}