
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.ViewModels
{
    public class CreateTaskViewModel
    {
        [Required(ErrorMessage = "Tiêu đề công việc không được để trống.")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được quá 200 ký tự.")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Ngày hết hạn")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }
    }
}