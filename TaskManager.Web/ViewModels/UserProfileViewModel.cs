using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.ViewModels
{
    public class UserProfileViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Tên đăng nhập")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [MaxLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [Display(Name = "Ngày tham gia")]
        public string? CreatedAtString { get; set; }
    }
}