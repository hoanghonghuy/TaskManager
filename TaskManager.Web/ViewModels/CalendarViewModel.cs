using System;
using System.Collections.Generic;
using TaskManager.Data.Models;

namespace TaskManager.Web.ViewModels
{
    public class CalendarViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = null!;
        public List<DateTime> DaysInGrid { get; set; } = new List<DateTime>(); 
        public Dictionary<DateTime, List<TaskManager.Data.Models.Task>> TasksByDay { get; set; } = new Dictionary<DateTime, List<TaskManager.Data.Models.Task>>();
    }
}