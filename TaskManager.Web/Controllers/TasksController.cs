using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.Data;
using TaskManager.Web.ViewModels;

namespace TaskManager.Web.Controllers
{
    [Authorize] // Chỉ những người dùng đã đăng nhập mới có thể truy cập
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy User ID của người dùng hiện tại
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Truy vấn danh sách công việc của người dùng hiện tại
            var userTasks = await _context.Tasks
                .Where(t => t.UserId == int.Parse(userId))
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(userTasks);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateTaskViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Lấy User ID của người dùng hiện tại
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    // Xử lý lỗi nếu không tìm thấy User ID
                    return RedirectToAction("Login", "Account");
                }

                var newTask = new TaskManager.Data.Models.Task 
                {
                    Title = model.Title,
                    Description = model.Description,
                    DueDate = model.DueDate,
                    Status = "Pending", // Gán trạng thái mặc định
                    CreatedAt = DateTime.UtcNow,
                    UserId = int.Parse(userId) // Chuyển đổi ID từ string sang int
                };

                _context.Tasks.Add(newTask);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            // Nếu dữ liệu không hợp lệ, trả về View với lỗi
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            // Kiểm tra xem công việc có thuộc về người dùng hiện tại không
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (task.UserId != int.Parse(userId))
            {
                return Forbid(); // Trả về lỗi 403 Forbidden
            }

            // Chuyển đổi từ Model thành ViewModel để hiển thị ra View
            var model = new EditTaskViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditTaskViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem công việc có thuộc về người dùng hiện tại không
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var taskToUpdate = await _context.Tasks.FindAsync(model.Id);

                if (taskToUpdate == null || taskToUpdate.UserId != int.Parse(userId))
                {
                    return NotFound();
                }

                taskToUpdate.Title = model.Title;
                taskToUpdate.Description = model.Description;
                taskToUpdate.DueDate = model.DueDate;

                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            // Kiểm tra phân quyền: Đảm bảo công việc thuộc về người dùng hiện tại
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (task.UserId != int.Parse(userId))
            {
                return Forbid();
            }

            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kiểm tra phân quyền một lần nữa trước khi xóa
            if (task == null || task.UserId != int.Parse(userId))
            {
                return NotFound(); // Hoặc Forbid() tùy vào cách xử lý lỗi
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}