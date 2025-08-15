using TaskManager.Data.Models;
using System.Collections.Generic;

namespace TaskManager.Web.ViewModels
{
    public class EditUserRolesViewModel
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }

        // Danh sách TẤT CẢ các vai trò có trong hệ thống
        public List<Role> AllRoles { get; set; } = new List<Role>();

        // Danh sách tên của các vai trò mà người dùng HIỆN CÓ
        public IList<string> UserRoles { get; set; } = new List<string>();
    }
}