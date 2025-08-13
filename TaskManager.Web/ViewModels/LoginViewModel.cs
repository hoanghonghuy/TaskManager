using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập hoặc Email không được để trống.")]
        [Display(Name = "Tên đăng nhập hoặc Email")]
        public required string UsernameOrEmail { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
    }
}