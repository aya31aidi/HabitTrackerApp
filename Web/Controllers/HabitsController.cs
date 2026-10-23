using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HabitTrackerApp.Models;
using Web.Data;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class HabitsController : AppController
    {
        // Half a year of full weeks fits nicely in the heatmap on a laptop screen
        private const int CalendarWeeks = 26;

        private readonly WebContext _context;
        private readonly HabitStatsService _stats;

        public HabitsController(WebContext context, HabitStatsService stats)
        {
            _context = context;
            _stats = stats;
        }

        // GET: Habits?categoryId=2
        public async Task<IActionResult> Index(int? categoryId)
        {
            var model = new HabitListViewModel
            {
                Habits = await _stats.GetSummariesAsync(User, categoryId),
                Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                SelectedCategoryId = categoryId,
                IsAdmin = IsAdmin
            };

            return View(model);
        }

        // Old links pointed here, the filter now lives on the Index page
        public IActionResult ByCategory(int id) => RedirectToAction(nameof(Index), new { categoryId = id });

        // GET: Habits/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var habit = await _stats.VisibleHabits(User)
                .Include(h => h.Category)
                .Include(h => h.User)
                .Include(h => h.HabitRecords)
                .Include(h => h.Reminders)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (habit == null)
            {
                return NotFound();
            }

            var today = DateTime.Today;
            var summary = HabitStatsService.Summarize(habit, habit.HabitRecords, today);

            // Start the calendar on a Monday so every column is a full week
            var start = today.AddDays(-(CalendarWeeks * 7 - 1));
            start = start.AddDays(-(((int)start.DayOfWeek + 6) % 7));

            var completed = habit.HabitRecords.Where(r => r.IsCompleted).Select(r => r.Date.Date).ToHashSet();
            var recorded = habit.HabitRecords.Select(r => r.Date.Date).ToHashSet();
            var calendar = new List<DayStatus>();
            for (var day = start; day <= today; day = day.AddDays(1))
            {
                calendar.Add(new DayStatus(day, completed.Contains(day), recorded.Contains(day)));
            }

            var model = new HabitDetailsViewModel
            {
                Summary = summary,
                Calendar = calendar,
                RecentRecords = habit.HabitRecords.OrderByDescending(r => r.Date).Take(8).ToList(),
                Reminders = habit.Reminders.Where(r => r.ReminderTime >= DateTime.Now).OrderBy(r => r.ReminderTime).ToList()
            };

            return View(model);
        }

        // GET: Habits/Create
        public async Task<IActionResult> Create(int? categoryId)
        {
            await FillSelectListsAsync(categoryId, null);
            return View(new Habit { CategoryId = categoryId ?? 0 });
        }

        // POST: Habits/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,CategoryId,Goal,UserId")] Habit habit)
        {
            if (!IsAdmin)
            {
                // A regular user always creates habits on their own account
                habit.UserId = CurrentUserId;
                ModelState.Remove(nameof(Habit.UserId));
            }

            await ValidateReferencesAsync(habit);

            if (!ModelState.IsValid)
            {
                await FillSelectListsAsync(habit.CategoryId, habit.UserId);
                return View(habit);
            }

            _context.Add(habit);
            await _context.SaveChangesAsync();

            NotifySuccess($"Habit \"{habit.Name}\" created. Check it off once it's done today!");
            return RedirectToAction(nameof(Index));
        }

        // GET: Habits/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var habit = await _stats.VisibleHabits(User).FirstOrDefaultAsync(h => h.Id == id);
            if (habit == null)
            {
                return NotFound();
            }

            await FillSelectListsAsync(habit.CategoryId, habit.UserId);
            return View(habit);
        }

        // POST: Habits/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,CategoryId,Goal,UserId")] Habit habit)
        {
            if (id != habit.Id)
            {
                return NotFound();
            }

            var existing = await _stats.VisibleHabits(User).AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            if (!IsAdmin)
            {
                // Force UserId to stay the owner's one
                habit.UserId = existing.UserId;
                ModelState.Remove(nameof(Habit.UserId));
            }

            await ValidateReferencesAsync(habit);

            if (!ModelState.IsValid)
            {
                await FillSelectListsAsync(habit.CategoryId, habit.UserId);
                return View(habit);
            }

            try
            {
                _context.Update(habit);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HabitExists(habit.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            NotifySuccess("Habit updated.");
            return RedirectToAction(nameof(Details), new { id = habit.Id });
        }

        // GET: Habits/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var habit = await _stats.VisibleHabits(User)
                .Include(h => h.Category)
                .Include(h => h.User)
                .Include(h => h.HabitRecords)
                .Include(h => h.Reminders)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (habit == null)
            {
                return NotFound();
            }

            return View(habit);
        }

        // POST: Habits/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var habit = await _stats.VisibleHabits(User).FirstOrDefaultAsync(h => h.Id == id);
            if (habit != null)
            {
                // Records, statistic and reminders go away with it (cascade delete)
                _context.Habits.Remove(habit);
                await _context.SaveChangesAsync();

                NotifySuccess("The habit has been successfully deleted.");
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Habits/Toggle/5 (called with fetch from site.js)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id, DateTime? date, string? returnUrl)
        {
            var habit = await _stats.VisibleHabits(User).AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);
            if (habit == null)
            {
                return NotFound();
            }

            var day = (date ?? DateTime.Today).Date;
            if (day > DateTime.Today)
            {
                return BadRequest(new { message = "Future days can't be checked yet." });
            }

            var completed = await _stats.ToggleAsync(id, day);

            var records = await _context.HabitRecords.AsNoTracking().Where(r => r.HabitId == id).ToListAsync();
            var summary = HabitStatsService.Summarize(habit, records, DateTime.Today);

            // Plain form post fallback when JavaScript is off
            if (!IsAjaxRequest())
            {
                NotifySuccess(completed ? $"Nice! \"{habit.Name}\" marked as done." : $"\"{habit.Name}\" unchecked.");
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? LocalRedirect(returnUrl)
                    : RedirectToAction(nameof(Index));
            }

            return Json(new
            {
                completed,
                date = day.ToString("yyyy-MM-dd"),
                doneToday = summary.DoneToday,
                streak = summary.CurrentStreak,
                bestStreak = summary.BestStreak,
                rate = summary.CompletionRate,
                total = summary.TotalCompletions,
                name = habit.Name
            });
        }

        private async Task ValidateReferencesAsync(Habit habit)
        {
            if (!await _context.Categories.AnyAsync(c => c.Id == habit.CategoryId))
            {
                ModelState.AddModelError(nameof(Habit.CategoryId), "Please pick a category.");
            }

            if (IsAdmin && !await _context.Users.AnyAsync(u => u.Id == habit.UserId))
            {
                ModelState.AddModelError(nameof(Habit.UserId), "Please pick the user who owns this habit.");
            }
        }

        private async Task FillSelectListsAsync(int? categoryId, int? userId)
        {
            ViewData["CategoryId"] = new SelectList(await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", categoryId);

            if (IsAdmin)
            {
                // Admin selects user
                ViewData["UserId"] = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Email", userId);
            }
        }

        private bool HabitExists(int id)
        {
            return _context.Habits.Any(e => e.Id == id);
        }
    }
}
