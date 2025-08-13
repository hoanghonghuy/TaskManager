using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace TaskManager.Data.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<Task> Tasks { get; set; } = new List<Task>();
    }
}