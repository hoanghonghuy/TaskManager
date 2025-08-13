using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Web.Models;
using TaskManager.Web.ViewModels;

namespace TaskManager.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                var tasks = await _context.Tasks
                    .Where(t => t.UserId == int.Parse(userId))
                    .ToListAsync();

                var viewModel = new DashboardViewModel
                {
                    TotalTasks = tasks.Count,
                    PendingTasks = tasks.Count(t => t.Status == "Pending"),
                    CompletedTasks = tasks.Count(t => t.Status == "Completed")
                };

                return View(viewModel);
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}