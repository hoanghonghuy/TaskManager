using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.Data;
using TaskManager.Web.ViewModels;
using TaskManager.Data.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
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
                .Where(t => t.UserId == int.Parse(userId!) && t.ParentTaskId == null);

            // --- Các bộ lọc ---
            ViewBag.ProjectId = projectId;
            ViewBag.Filter = status;
            ViewBag.SearchString = searchString;

            if (projectId.HasValue)
            {
                userTasksQuery = userTasksQuery.Where(t => t.ProjectId == projectId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                userTasksQuery = userTasksQuery.Where(t => t.Status == status);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                userTasksQuery = userTasksQuery.Where(t => t.Title!.Contains(searchString) || (t.Description != null && t.Description.Contains(searchString)));
            }

            // --- LỌC THÔNG MINH CÁC CÔNG VIỆC LẶP LẠI ---
            var today = DateTime.UtcNow.Date;
            userTasksQuery = userTasksQuery
                .Where(t =>
                    string.IsNullOrEmpty(t.RecurrenceRule) ||
                    t.Status != "Completed" ||
                    (t.Status == "Completed" && t.DueDate >= today.AddDays(-7))
                );

            // --- Sắp xếp ---
            ViewBag.DateSortParam = string.IsNullOrEmpty(sortOrder) || sortOrder == "date_desc" ? "date_asc" : "date_desc";
            ViewBag.PrioritySortParam = sortOrder == "priority_asc" ? "priority_desc" : "priority_asc";

            switch (sortOrder)
            {
                case "date_asc":
                    userTasksQuery = userTasksQuery.OrderBy(t => t.DueDate ?? t.CreatedAt);
                    break;
                case "priority_desc":
                    userTasksQuery = userTasksQuery.OrderByDescending(t => t.Priority == "High" ? 3 : t.Priority == "Medium" ? 2 : t.Priority == "Low" ? 1 : 0);
                    break;
                case "priority_asc":
                    userTasksQuery = userTasksQuery.OrderBy(t => t.Priority == "High" ? 3 : t.Priority == "Medium" ? 2 : t.Priority == "Low" ? 1 : 0);
                    break;
                default: // Mặc định là date_desc
                    userTasksQuery = userTasksQuery.OrderByDescending(t => t.DueDate ?? t.CreatedAt);
                    break;
            }

            var userTasks = await userTasksQuery.ToListAsync();
            return View(userTasks);
        }
        #endregion

        #region Create
        [HttpGet]
        public async Task<IActionResult> Create(int? parentTaskId, [FromQuery] string? title, [FromQuery] string? dueDate, [FromQuery] string? priority, [FromQuery] string? tags)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var projects = await _context.Projects.Where(p => p.UserId == int.Parse(userId!)).ToListAsync();

            var viewModel = new CreateTaskViewModel
            {
                Projects = projects,
                ParentTaskId = parentTaskId,
                
                Title = title ?? string.Empty,
                Priority = priority ?? "None",
                TagNames = tags ?? string.Empty
            };

            if (DateTime.TryParse(dueDate, out var parsedDueDate))
            {
                viewModel.DueDate = parsedDueDate;
            }

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

                // --- XỬ LÝ THẺ (TAGS) ---
                var tagNames = model.TagNames.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
                var tagsToAssociate = await _context.Tags.Where(t => tagNames.Contains(t.Name)).ToListAsync();
                foreach (var tagName in tagNames.Except(tagsToAssociate.Select(t => t.Name)))
                {
                    var newTag = new Tag { Name = tagName };
                    _context.Tags.Add(newTag);
                    tagsToAssociate.Add(newTag);
                }

                // --- XỬ LÝ THỜI GIAN NHẮC NHỞ ---
                DateTime? reminderTime = null;
                if (!string.IsNullOrEmpty(model.ReminderTimeString) && DateTime.TryParse(model.ReminderTimeString, out var parsedTime))
                {
                    reminderTime = parsedTime.ToUniversalTime();
                }

                // --- TẠO CÔNG VIỆC GỐC ---
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
                    RecurrenceEndDate = model.RecurrenceEndDate,
                    ReminderTime = reminderTime
                };

                foreach (var tag in tagsToAssociate)
                {
                    newTask.TaskTags.Add(new TaskTag { Tag = tag });
                }
                _context.Tasks.Add(newTask);

                // --- TẠO CÁC CÔNG VIỆC LẶP LẠI  ---
                if (!string.IsNullOrEmpty(model.RecurrenceRule) && model.RecurrenceEndDate.HasValue && model.DueDate.HasValue)
                {
                    var currentDate = model.DueDate.Value;
                    while (true)
                    {
                        switch (model.RecurrenceRule)
                        {
                            case "Daily": currentDate = currentDate.AddDays(1); break;
                            case "Weekly": currentDate = currentDate.AddDays(7); break;
                            case "Monthly": currentDate = currentDate.AddMonths(1); break;
                            case "Yearly": currentDate = currentDate.AddYears(1); break;
                            default: goto EndLoop;
                        }
                        if (currentDate > model.RecurrenceEndDate.Value) break;

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
                            RecurrenceEndDate = model.RecurrenceEndDate,
                            ReminderTime = reminderTime
                        };
                        foreach (var tag in tagsToAssociate)
                        {
                            recurringTask.TaskTags.Add(new TaskTag { Tag = tag });
                        }
                        _context.Tasks.Add(recurringTask);
                    }
                EndLoop:;
                }

                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" && newTask.ParentTaskId.HasValue)
                {
                    
                    return PartialView("_SubtaskItem", newTask);
                }
                TempData["success"] = "Công việc đã được tạo thành công!";
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
            if (task == null || task.UserId != int.Parse(userId!)) return NotFound();

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
                RecurrenceEndDate = task.RecurrenceEndDate,

                
                // Chuyển đổi DateTime từ UTC (trong DB) sang giờ địa phương và định dạng lại thành chuỗi "yyyy-MM-ddTHH:mm" mà input datetime-local yêu cầu.
                ReminderTimeString = task.ReminderTime?.ToLocalTime().ToString("yyyy-MM-ddTHH:mm")
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
                var taskToUpdate = await _context.Tasks.Include(t => t.TaskTags)!.ThenInclude(t => t.Tag).FirstOrDefaultAsync(t => t.Id == model.Id);

                if (taskToUpdate == null || taskToUpdate.UserId != int.Parse(userId!)) return NotFound();

                // Cập nhật các thuộc tính
                taskToUpdate.Title = model.Title;
                taskToUpdate.Description = model.Description;
                taskToUpdate.DueDate = model.DueDate;
                taskToUpdate.Priority = model.Priority;
                taskToUpdate.ProjectId = model.ProjectId;
                taskToUpdate.RecurrenceRule = model.RecurrenceRule;
                taskToUpdate.RecurrenceEndDate = model.RecurrenceEndDate;

                // Cập nhật ReminderTime
                if (!string.IsNullOrEmpty(model.ReminderTimeString) && DateTime.TryParse(model.ReminderTimeString, out var parsedTime))
                {
                    taskToUpdate.ReminderTime = parsedTime.ToUniversalTime();
                }
                else
                {
                    taskToUpdate.ReminderTime = null;
                }

                // Cập nhật lại trạng thái IsReminded nếu thời gian nhắc nhở thay đổi
                taskToUpdate.IsReminded = false;

                // Cập nhật Tags
                var tagNames = model.TagNames.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
                var tagsToAssociate = await _context.Tags.Where(t => tagNames.Contains(t.Name)).ToListAsync();
                foreach (var tagName in tagNames.Except(tagsToAssociate.Select(t => t.Name)))
                {
                    var newTag = new Tag { Name = tagName };
                    _context.Tags.Add(newTag);
                    tagsToAssociate.Add(newTag);
                }

                // Xóa các tag cũ và thêm các tag mới
                taskToUpdate.TaskTags.Clear();
                await _context.SaveChangesAsync(); // Lưu để xóa các tag cũ

                foreach (var tag in tagsToAssociate)
                {
                    taskToUpdate.TaskTags.Add(new TaskTag { Task = taskToUpdate, Tag = tag });
                }
                
                await _context.SaveChangesAsync();
                TempData["success"] = "Công việc đã được cập nhật!";
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.Tasks.FindAsync(id);
            if (task == null || task.UserId != int.Parse(userId!)) return NotFound();
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            TempData["success"] = "Công việc đã được xóa thành công!";
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

            return Json(new { success = true, newStatus = task.Status, taskId = task.Id });
        }
        #endregion

        #region Actions for Task Details
        public async Task<IActionResult> Details(int? id, bool isPartial = false)
        {
            if (id == null) { return isPartial ? PartialView("_TaskDetailsPartial", null) : NotFound(); }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskTags).ThenInclude(tt => tt.Tag)
                
                .Include(t => t.Subtasks) 
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == int.Parse(userId!));

            if (task == null) { return isPartial ? PartialView("_TaskDetailsPartial", null) : NotFound(); }

            return isPartial ? PartialView("_TaskDetailsPartial", task) : View(task);
        }
        #endregion
    }
}