using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.Data;
using TaskManager.Web.ViewModels;
using TaskManager.Data.Models;
using BCrypt.Net;
using System.Diagnostics;
using System.Collections.Generic;

namespace TaskManager.Web.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region View, Search & Sort
        public async Task<IActionResult> Index(string? status, string? searchString, string? sortOrder, int? projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IQueryable<TaskManager.Data.Models.Task> userTasksQuery = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskTags)!
                    .ThenInclude(tt => tt.Tag)
                .Where(t => t.UserId == int.Parse(userId!));

            if (projectId.HasValue)
            {
                userTasksQuery = userTasksQuery.Where(t => t.ProjectId == projectId.Value);
                ViewBag.ProjectId = projectId.Value;
            }

            if (!string.IsNullOrEmpty(status))
            {
                userTasksQuery = userTasksQuery.Where(t => t.Status == status);
                ViewBag.Filter = status;
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                userTasksQuery = userTasksQuery.Where(t => t.Title!.Contains(searchString) || (t.Description != null && t.Description.Contains(searchString)));
                ViewBag.SearchString = searchString;
            }

            ViewBag.DateSortParam = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewBag.PrioritySortParam = sortOrder == "priority" ? "priority_desc" : "priority";

            switch (sortOrder)
            {
                case "date_desc":
                    userTasksQuery = userTasksQuery.OrderByDescending(t => t.CreatedAt);
                    break;
                case "priority":
                    userTasksQuery = userTasksQuery.OrderBy(t => t.Priority);
                    break;
                case "priority_desc":
                    userTasksQuery = userTasksQuery.OrderByDescending(t => t.Priority);
                    break;
                default:
                    userTasksQuery = userTasksQuery.OrderBy(t => t.CreatedAt);
                    break;
            }

            var userTasks = await userTasksQuery.ToListAsync();
            return View(userTasks);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskTags)!
                    .ThenInclude(tt => tt.Tag)
                .Include(t => t.Subtasks)
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == int.Parse(userId!));

            if (task == null) return NotFound();
            return View(task);
        }
        #endregion

        #region Create
        [HttpGet]
        public async Task<IActionResult> Create(int? parentTaskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var projects = await _context.Projects.Where(p => p.UserId == int.Parse(userId!)).ToListAsync();
            var viewModel = new CreateTaskViewModel { Projects = projects, ParentTaskId = parentTaskId };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTaskViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null) return RedirectToAction("Login", "Account");

                var newTask = new TaskManager.Data.Models.Task
                {
                    Title = model.Title,
                    Description = model.Description,
                    DueDate = model.DueDate,
                    Status = "Pending",
                    Priority = model.Priority,
                    CreatedAt = DateTime.UtcNow,
                    UserId = int.Parse(userId!),
                    ProjectId = model.ProjectId,
                    ParentTaskId = model.ParentTaskId,
                    RecurrenceRule = model.RecurrenceRule,
                    RecurrenceEndDate = model.RecurrenceEndDate
                };

                // Xử lý thẻ
                var tagNames = model.TagNames.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
                var existingTags = await _context.Tags.Where(t => tagNames.Contains(t.Name)).ToListAsync();
                foreach (var tagName in tagNames.Except(existingTags.Select(t => t.Name)))
                {
                    existingTags.Add(new Tag { Name = tagName });
                }
                foreach (var tag in existingTags)
                {
                    newTask.TaskTags.Add(new TaskTag { Tag = tag });
                }

                _context.Tasks.Add(newTask);

                // Xử lý công việc lặp lại
                if (!string.IsNullOrEmpty(model.RecurrenceRule) && model.RecurrenceEndDate.HasValue && model.DueDate.HasValue)
                {
                    var currentDate = model.DueDate.Value;
                    while (currentDate <= model.RecurrenceEndDate.Value)
                    {
                        if (currentDate != model.DueDate.Value)
                        {
                            var recurringTask = new TaskManager.Data.Models.Task
                            {
                                Title = model.Title,
                                Description = model.Description,
                                DueDate = currentDate,
                                Status = "Pending",
                                Priority = model.Priority,
                                CreatedAt = DateTime.UtcNow,
                                UserId = int.Parse(userId!),
                                ProjectId = model.ProjectId,
                                RecurrenceRule = model.RecurrenceRule,
                                RecurrenceEndDate = model.RecurrenceEndDate
                            };

                            foreach (var tag in existingTags)
                            {
                                recurringTask.TaskTags.Add(new TaskTag { Tag = tag });
                            }
                            _context.Tasks.Add(recurringTask);
                        }

                        switch (model.RecurrenceRule)
                        {
                            case "Daily":
                                currentDate = currentDate.AddDays(1);
                                break;
                            case "Weekly":
                                currentDate = currentDate.AddDays(7);
                                break;
                            case "Monthly":
                                currentDate = currentDate.AddMonths(1);
                                break;
                            case "Yearly":
                                currentDate = currentDate.AddYears(1);
                                break;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                if (newTask.ParentTaskId.HasValue)
                {
                    return RedirectToAction("Details", new { id = newTask.ParentTaskId.Value });
                }

                return RedirectToAction("Index");
            }
            var userIdForProjects = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.Projects = await _context.Projects.Where(p => p.UserId == int.Parse(userIdForProjects!)).ToListAsync();
            return View(model);
        }
        #endregion

        #region Edit
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.Tasks.Include(t => t.TaskTags)!.ThenInclude(tt => tt.Tag).FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();
            if (task.UserId != int.Parse(userId!)) return Forbid();

            var projects = await _context.Projects.Where(p => p.UserId == int.Parse(userId!)).ToListAsync();

            var model = new EditTaskViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Priority = task.Priority,
                ProjectId = task.ProjectId,
                Projects = projects,
                TagNames = string.Join(", ", task.TaskTags.Select(tt => tt.Tag.Name)),
                RecurrenceRule = task.RecurrenceRule,
                RecurrenceEndDate = task.RecurrenceEndDate
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditTaskViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var taskToUpdate = await _context.Tasks.Include(t => t.TaskTags)!.FirstOrDefaultAsync(t => t.Id == model.Id);

                if (taskToUpdate == null || taskToUpdate.UserId != int.Parse(userId!)) return NotFound();

                taskToUpdate.Title = model.Title;
                taskToUpdate.Description = model.Description;
                taskToUpdate.DueDate = model.DueDate;
                taskToUpdate.Priority = model.Priority;
                taskToUpdate.ProjectId = model.ProjectId;
                taskToUpdate.RecurrenceRule = model.RecurrenceRule;
                taskToUpdate.RecurrenceEndDate = model.RecurrenceEndDate;

                var tagNames = model.TagNames.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
                var existingTags = await _context.Tags.Where(t => tagNames.Contains(t.Name)).ToListAsync();
                foreach (var tagName in tagNames.Except(existingTags.Select(t => t.Name)))
                {
                    existingTags.Add(new Tag { Name = tagName });
                }

                taskToUpdate.TaskTags.Clear();
                foreach (var tag in existingTags)
                {
                    taskToUpdate.TaskTags.Add(new TaskTag { Tag = tag });
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            var userIdForProjects = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.Projects = await _context.Projects.Where(p => p.UserId == int.Parse(userIdForProjects!)).ToListAsync();
            return View(model);
        }
        #endregion

        #region Delete & Update Status
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.Tasks.FirstOrDefaultAsync(m => m.Id == id && m.UserId == int.Parse(userId!));
            if (task == null) return NotFound();
            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (task == null || task.UserId != int.Parse(userId!)) return NotFound();
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTaskStatus(int? id)
        {
            if (id == null) return NotFound();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var task = await _context.Tasks.FindAsync(id);
            if (task == null || task.UserId != int.Parse(userId!)) return NotFound();
            task.Status = task.Status == "Completed" ? "Pending" : "Completed";
            _context.Update(task);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        #endregion
    }
}