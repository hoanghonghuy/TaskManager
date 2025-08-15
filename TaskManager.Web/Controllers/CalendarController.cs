using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using TaskManager.Data;
using TaskManager.Web.ViewModels; 

namespace TaskManager.Web.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CalendarController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? month, int? year)
        {
            var today = DateTime.Today;
            var currentMonth = month ?? today.Month;
            var currentYear = year ?? today.Year;

            // Đảm bảo tháng và năm hợp lệ
            if (currentMonth < 1) { currentMonth = 12; currentYear--; }
            if (currentMonth > 12) { currentMonth = 1; currentYear++; }

            var startDateOfMonth = new DateTime(currentYear, currentMonth, 1);

            // Tính ngày bắt đầu của ô lịch đầu tiên (có thể là của tháng trước)
            var firstDayOfWeek = (int)startDateOfMonth.DayOfWeek;
            // Điều chỉnh để Thứ Hai là ngày đầu tuần (1) và Chủ Nhật là cuối tuần (7)
            var daysToSubtract = (firstDayOfWeek == 0) ? 6 : firstDayOfWeek - 1;
            var calendarStartDate = startDateOfMonth.AddDays(-daysToSubtract);

            // Tạo danh sách 35 ngày (5 hàng x 7 ngày) để hiển thị trên lịch
            var daysInGrid = Enumerable.Range(0, 35).Select(i => calendarStartDate.AddDays(i)).ToList();
            var calendarEndDate = daysInGrid.Last();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tasks = await _context.Tasks
                .Where(t => t.UserId == int.Parse(userId!) &&
                            t.DueDate.HasValue &&
                            t.DueDate.Value.Date >= daysInGrid.First().Date &&
                            t.DueDate.Value.Date <= calendarEndDate.Date)
                .ToListAsync();

            // Nhóm các công việc theo ngày để dễ dàng hiển thị
            var tasksByDay = tasks
                .GroupBy(t => t.DueDate!.Value.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            var viewModel = new CalendarViewModel
            {
                Year = currentYear,
                Month = currentMonth,
                MonthName = startDateOfMonth.ToString("MMMM", new CultureInfo("vi-VN")),
                DaysInGrid = daysInGrid,
                TasksByDay = tasksByDay
            };

            return View(viewModel);
        }
    }
}