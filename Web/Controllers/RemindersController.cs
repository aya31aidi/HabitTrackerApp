using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HabitTrackerApp.Models;
using Web.Data;
using Web.Services;

namespace Web.Controllers
{
    public class RemindersController : AppController
    {
        private readonly WebContext _context;
        private readonly HabitStatsService _stats;

        public RemindersController(WebContext context, HabitStatsService stats)
        {
            _context = context;
            _stats = stats;
        }

        // GET: Reminders
        public async Task<IActionResult> Index()
        {
            var reminders = await VisibleReminders()
                .Include(r => r.Habit)
                    .ThenInclude(h => h!.Category)
                .Include(r => r.User)
                .OrderBy(r => r.ReminderTime)
                .ToListAsync();

            ViewBag.IsAdmin = IsAdmin;
            return View(reminders);
        }

        // GET: Reminders/Upcoming : polled by site.js to raise browser notifications
        [HttpGet]
        public async Task<IActionResult> Upcoming()
        {
            var now = DateTime.Now;
            var reminders = await VisibleReminders()
                .Include(r => r.Habit)
                .Where(r => r.ReminderTime >= now.AddMinutes(-1) && r.ReminderTime <= now.AddHours(24))
                .OrderBy(r => r.ReminderTime)
                .Select(r => new
                {
                    id = r.Id,
                    message = r.Message,
                    habit = r.Habit!.Name,
                    time = r.ReminderTime.ToString("yyyy-MM-ddTHH:mm:ss")
                })
                .ToListAsync();

            return Json(reminders);
        }

        // GET: Reminders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reminder = await VisibleReminders()
                .Include(r => r.Habit)
                    .ThenInclude(h => h!.Category)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reminder == null)
            {
                return NotFound();
            }

            return View(reminder);
        }

        // GET: Reminders/Create?habitId=3
        public async Task<IActionResult> Create(int? habitId)
        {
            await FillHabitsAsync(habitId);

            // Suggest the next round hour so the picker doesn't start in the past
            var suggestion = DateTime.Now.Date.AddHours(DateTime.Now.Hour + 1);
            return View(new Reminder { HabitId = habitId ?? 0, ReminderTime = suggestion });
        }

        // POST: Reminders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Message,ReminderTime,HabitId")] Reminder reminder)
        {
            var habit = await _stats.VisibleHabits(User).FirstOrDefaultAsync(h => h.Id == reminder.HabitId);
            if (habit == null)
            {
                ModelState.AddModelError(nameof(Reminder.HabitId), "Please pick one of the habits.");
            }

            if (!ModelState.IsValid)
            {
                await FillHabitsAsync(reminder.HabitId);
                return View(reminder);
            }

            // The reminder always belongs to the owner of the habit
            reminder.UserId = habit!.UserId;

            _context.Add(reminder);
            await _context.SaveChangesAsync();

            NotifySuccess("Reminder scheduled.");
            return RedirectToAction(nameof(Index));
        }

        // GET: Reminders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reminder = await VisibleReminders().FirstOrDefaultAsync(r => r.Id == id);
            if (reminder == null)
            {
                return NotFound();
            }

            await FillHabitsAsync(reminder.HabitId);
            return View(reminder);
        }

        // POST: Reminders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Message,ReminderTime,HabitId")] Reminder reminder)
        {
            if (id != reminder.Id)
            {
                return NotFound();
            }

            if (!await VisibleReminders().AnyAsync(r => r.Id == id))
            {
                return NotFound();
            }

            var habit = await _stats.VisibleHabits(User).FirstOrDefaultAsync(h => h.Id == reminder.HabitId);
            if (habit == null)
            {
                ModelState.AddModelError(nameof(Reminder.HabitId), "Please pick one of the habits.");
            }

            if (!ModelState.IsValid)
            {
                await FillHabitsAsync(reminder.HabitId);
                return View(reminder);
            }

            reminder.UserId = habit!.UserId;

            try
            {
                _context.Update(reminder);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReminderExists(reminder.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            NotifySuccess("Reminder updated.");
            return RedirectToAction(nameof(Index));
        }

        // GET: Reminders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reminder = await VisibleReminders()
                .Include(r => r.Habit)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reminder == null)
            {
                return NotFound();
            }

            return View(reminder);
        }

        // POST: Reminders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reminder = await VisibleReminders().FirstOrDefaultAsync(r => r.Id == id);
            if (reminder != null)
            {
                _context.Reminders.Remove(reminder);
                await _context.SaveChangesAsync();

                NotifySuccess("The reminder has been successfully deleted.");
            }
            return RedirectToAction(nameof(Index));
        }

        private IQueryable<Reminder> VisibleReminders()
        {
            if (IsAdmin)
                return _context.Reminders;

            var userId = CurrentUserId;
            return _context.Reminders.Where(r => r.UserId == userId);
        }

        private async Task FillHabitsAsync(int? selectedId)
        {
            var habits = await _stats.VisibleHabits(User)
                .Include(h => h.User)
                .OrderBy(h => h.Name)
                .ToListAsync();

            // Show the habit name (the goal was shown before, which is often empty)
            var items = habits.Select(h => new { h.Id, Label = IsAdmin ? $"{h.Name} ({h.User?.Name})" : h.Name });
            ViewData["HabitId"] = new SelectList(items, "Id", "Label", selectedId);
        }

        private bool ReminderExists(int id)
        {
            return _context.Reminders.Any(e => e.Id == id);
        }
    }
}
