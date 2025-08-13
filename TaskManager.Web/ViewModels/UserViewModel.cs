using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = null!;

        [StringLength(100, ErrorMessage = "Tên đầy đủ không được vượt quá 100 ký tự.")]
        public string? FullName { get; set; }
    }
}