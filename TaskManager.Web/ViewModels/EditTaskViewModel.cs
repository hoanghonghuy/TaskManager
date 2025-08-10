using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.ViewModels
{
    public class EditTaskViewModel
    {
        public int Id { get; set; } // Cần có ID để biết công việc nào cần sửa

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