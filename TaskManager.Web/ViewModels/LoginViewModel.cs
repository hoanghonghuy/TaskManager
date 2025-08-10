using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập hoặc Email không được để trống.")]
        [Display(Name = "Tên đăng nhập hoặc Email")]
        public string UsernameOrEmail { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}