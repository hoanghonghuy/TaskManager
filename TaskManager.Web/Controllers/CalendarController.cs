using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

            var startDate = new DateTime(currentYear, currentMonth, 1);
            var firstDayOfWeek = startDate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)startDate.DayOfWeek;
            var daysBefore = firstDayOfWeek - (int)DayOfWeek.Monday;
            var calendarStartDate = startDate.AddDays(-daysBefore);

            var daysInGrid = new List<DateTime>();
            for (int i = 0; i < 35; i++) // 5 hàng x 7 ngày = 35 ngày
            {
                daysInGrid.Add(calendarStartDate.AddDays(i));
            }
            var calendarEndDate = daysInGrid.Last();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tasks = await _context.Tasks
                .Where(t => t.UserId == int.Parse(userId!))
                .Where(t => t.DueDate >= calendarStartDate && t.DueDate <= calendarEndDate)
                .ToListAsync();

            var tasksByDay = tasks
                .GroupBy(t => t.DueDate!.Value.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            var viewModel = new CalendarViewModel
            {
                Year = currentYear,
                Month = currentMonth,
                MonthName = startDate.ToString("MMMM", new CultureInfo("vi-VN")),
                DaysInGrid = daysInGrid,
                TasksByDay = tasksByDay
            };

            return View(viewModel);
        }
    }
}