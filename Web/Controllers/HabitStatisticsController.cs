using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    /// <summary>
    /// Statistics used to be typed in by hand, now they are computed from the habit records.
    /// </summary>
    public class HabitStatisticsController : AppController
    {
        private const int TrendDays = 30;

        private readonly WebContext _context;
        private readonly HabitStatsService _stats;

        public HabitStatisticsController(WebContext context, HabitStatsService stats)
        {
            _context = context;
            _stats = stats;
        }

        // GET: HabitStatistics
        public async Task<IActionResult> Index()
        {
            var habits = await _stats.GetSummariesAsync(User);
            var from = DateTime.Today.AddDays(-TrendDays + 1);

            var completedDays = habits
                .SelectMany(h => h.Habit.HabitRecords)
                .Where(r => r.IsCompleted)
                .Select(r => new { r.HabitId, Day = r.Date.Date })
                .Distinct()
                .ToList();

            var perDay = completedDays
                .Where(r => r.Day >= from && r.Day <= DateTime.Today)
                .GroupBy(r => r.Day)
                .ToDictionary(g => g.Key, g => g.Count());

            var trend = Enumerable.Range(0, TrendDays)
                .Select(i => from.AddDays(i))
                .Select(day => new ChartPoint(day.ToString("dd MMM", CultureInfo.InvariantCulture), perDay.GetValueOrDefault(day)))
                .ToList();

            var byCategory = habits
                .GroupBy(h => h.Habit.Category?.Name ?? "Uncategorized")
                .Select(g => new ChartPoint(g.Key, g.Sum(h => h.TotalCompletions)))
                .OrderByDescending(p => p.Value)
                .ToList();

            // Monday first, like the heatmap
            var weekdayOrder = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            var byWeekday = weekdayOrder
                .Select(d => new ChartPoint(CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedDayName(d), completedDays.Count(r => r.Day.DayOfWeek == d)))
                .ToList();

            var model = new StatisticsViewModel
            {
                Habits = habits.OrderByDescending(h => h.CompletionRate).ToList(),
                Trend = trend,
                ByCategory = byCategory,
                ByWeekday = byWeekday,
                TrendDays = TrendDays
            };

            ViewBag.IsAdmin = IsAdmin;
            return View(model);
        }

        // GET: HabitStatistics/Details/5 : old statistic links now open the habit page
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var habitId = await _context.HabitStatistics
                .Where(s => s.Id == id)
                .Select(s => (int?)s.HabitId)
                .FirstOrDefaultAsync();

            return habitId == null
                ? NotFound()
                : RedirectToAction("Details", "Habits", new { id = habitId });
        }

        // POST: HabitStatistics/Recalculate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Recalculate()
        {
            await _stats.RefreshAllAsync(User);
            NotifySuccess("Statistics recalculated from the habit records.");
            return RedirectToAction(nameof(Index));
        }
    }
}
