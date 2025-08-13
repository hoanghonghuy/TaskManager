using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace TaskManager.Data.Models
{
    public class Tag
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    }
}