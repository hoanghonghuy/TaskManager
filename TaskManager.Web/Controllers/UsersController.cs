using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Data.Models;
using TaskManager.Web.ViewModels;

namespace TaskManager.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region User Management
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.UserRoles) 
                .ThenInclude(ur => ur.Role)
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    CreatedAt = u.CreatedAt,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList() 
                })
                .ToListAsync();

            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var allRoles = await _context.Roles.ToListAsync();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                AllRoles = allRoles,
                SelectedRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Nếu không hợp lệ, cần tải lại AllRoles để hiển thị lại form
                model.AllRoles = await _context.Roles.ToListAsync();
                return View(model);
            }

            var userToUpdate = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == model.Id);

            if (userToUpdate == null) return NotFound();

            // Cập nhật thông tin cơ bản
            userToUpdate.Email = model.Email;
            userToUpdate.FullName = model.FullName;

            // Cập nhật vai trò
            // 1. Xóa tất cả các vai trò cũ của người dùng
            userToUpdate.UserRoles.Clear();

            // 2. Thêm lại các vai trò mới được chọn từ form
            if (model.SelectedRoleIds != null)
            {
                foreach (var roleId in model.SelectedRoleIds)
                {
                    userToUpdate.UserRoles.Add(new UserRole { UserId = userToUpdate.Id, RoleId = roleId });
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var viewModel = new UserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt
            };
            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        #endregion
    }
}