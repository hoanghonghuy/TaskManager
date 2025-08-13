using System.ComponentModel.DataAnnotations;
using TaskManager.Data.Models;

namespace TaskManager.Web.ViewModels
{
    public class EditTaskViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề công việc không được để trống.")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được quá 200 ký tự.")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = null!;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Ngày hết hạn")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Required]
        [Display(Name = "Mức độ ưu tiên")]
        public string Priority { get; set; } = "None";
        public int? ProjectId { get; set; }
        public List<Project>? Projects { get; set; }
        public string TagNames { get; set; } = string.Empty;
    }
}