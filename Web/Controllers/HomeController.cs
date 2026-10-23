using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class HomeController : AppController
    {
        private const int ActivityDays = 14;

        private readonly WebContext _context;
        private readonly HabitStatsService _stats;

        public HomeController(WebContext context, HabitStatsService stats)
        {
            _context = context;
            _stats = stats;
        }

        public async Task<IActionResult> Index()
        {
            var habits = await _stats.GetSummariesAsync(User);

            var visibleIds = habits.Select(h => h.Habit.Id).ToList();
            var from = DateTime.Today.AddDays(-ActivityDays + 1);

            var completedPerDay = await _context.HabitRecords
                .Where(r => visibleIds.Contains(r.HabitId) && r.IsCompleted && r.Date >= from)
                .GroupBy(r => r.Date.Date)
                .Select(g => new { Day = g.Key, Count = g.Select(r => r.HabitId).Distinct().Count() })
                .ToDictionaryAsync(x => x.Day, x => x.Count);

            var activity = Enumerable.Range(0, ActivityDays)
                .Select(i => from.AddDays(i))
                .Select(day => new ChartPoint(day.ToString("ddd dd"), completedPerDay.GetValueOrDefault(day)))
                .ToList();

            var remindersQuery = _context.Reminders
                .Include(r => r.Habit)
                .Where(r => r.ReminderTime >= DateTime.Now);
            if (!IsAdmin)
                remindersQuery = remindersQuery.Where(r => r.UserId == CurrentUserId);

            var model = new DashboardViewModel
            {
                DisplayName = User.GetDisplayName(),
                IsAdmin = IsAdmin,
                Habits = habits,
                Activity = activity,
                UpcomingReminders = await remindersQuery.OrderBy(r => r.ReminderTime).Take(5).ToListAsync(),
                UserCount = IsAdmin ? await _context.Users.CountAsync() : 0,
                CategoryCount = IsAdmin ? await _context.Categories.CountAsync() : 0
            };

            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Status(int code)
        {
            ViewData["Code"] = code;
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
