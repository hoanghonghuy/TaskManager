using System.ComponentModel.DataAnnotations;
using TaskManager.Data.Models; 

namespace TaskManager.Web.ViewModels
{
    // ViewModel này dùng để hiển thị danh sách người dùng
    public class UserViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public DateTime CreatedAt { get; set; }
        public IList<string> Roles { get; set; } = new List<string>(); 
    }

    // ViewModel này dùng cho trang Sửa người dùng
    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = null!;

        [StringLength(100, ErrorMessage = "Tên đầy đủ không được vượt quá 100 ký tự.")]
        public string? FullName { get; set; }

        // Danh sách TẤT CẢ các vai trò có trong hệ thống (để hiển thị checkbox)
        public List<Role> AllRoles { get; set; } = new List<Role>();

        // Danh sách ID của các vai trò mà người dùng này ĐANG có
        public List<int> SelectedRoleIds { get; set; } = new List<int>();
    }
}