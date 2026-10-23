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
    public class HabitRecordsController : AppController
    {
        private readonly WebContext _context;
        private readonly HabitStatsService _stats;

        public HabitRecordsController(WebContext context, HabitStatsService stats)
        {
            _context = context;
            _stats = stats;
        }

        // GET: HabitRecords?habitId=3&status=done
        public async Task<IActionResult> Index(int? habitId, string? status)
        {
            var records = VisibleRecords()
                .Include(hr => hr.Habit)
                    .ThenInclude(h => h!.User)
                .Include(hr => hr.Habit)
                    .ThenInclude(h => h!.Category)
                .AsQueryable();

            if (habitId.HasValue)
            {
                records = records.Where(hr => hr.HabitId == habitId.Value);
            }

            if (status == "done")
            {
                records = records.Where(hr => hr.IsCompleted);
            }
            else if (status == "missed")
            {
                records = records.Where(hr => !hr.IsCompleted);
            }

            ViewBag.IsAdmin = IsAdmin;
            ViewBag.Status = status;
            ViewBag.HabitId = habitId;
            ViewBag.Habits = new SelectList(await _stats.VisibleHabits(User).OrderBy(h => h.Name).ToListAsync(), "Id", "Name", habitId);

            return View(await records.OrderByDescending(hr => hr.Date).ThenBy(hr => hr.Habit!.Name).ToListAsync());
        }

        // GET: HabitRecords/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var habitRecord = await VisibleRecords()
                .Include(h => h.Habit)
                    .ThenInclude(h => h!.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (habitRecord == null)
            {
                return NotFound();
            }

            return View(habitRecord);
        }

        // GET: HabitRecords/Create?habitId=3
        public async Task<IActionResult> Create(int? habitId)
        {
            await FillHabitsAsync(habitId);
            return View(new HabitRecord { Date = DateTime.Today, IsCompleted = true, HabitId = habitId ?? 0 });
        }

        // POST: HabitRecords/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Date,IsCompleted,HabitId")] HabitRecord habitRecord)
        {
            await ValidateRecordAsync(habitRecord);

            if (!ModelState.IsValid)
            {
                await FillHabitsAsync(habitRecord.HabitId);
                return View(habitRecord);
            }

            habitRecord.Date = habitRecord.Date.Date;

            // One record per habit and day: update the existing one instead of duplicating it
            var existing = await _context.HabitRecords
                .FirstOrDefaultAsync(r => r.HabitId == habitRecord.HabitId && r.Date == habitRecord.Date);
            if (existing != null)
            {
                existing.IsCompleted = habitRecord.IsCompleted;
                NotifySuccess("A record already existed for that day, it has been updated.");
            }
            else
            {
                _context.Add(habitRecord);
                NotifySuccess("Record saved.");
            }

            await _context.SaveChangesAsync();
            await _stats.RefreshStatisticAsync(habitRecord.HabitId);
            return RedirectToAction(nameof(Index));
        }

        // GET: HabitRecords/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var habitRecord = await VisibleRecords().FirstOrDefaultAsync(hr => hr.Id == id);
            if (habitRecord == null) return NotFound();

            await FillHabitsAsync(habitRecord.HabitId);
            return View(habitRecord);
        }

        // POST: HabitRecords/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Date,IsCompleted,HabitId")] HabitRecord habitRecord)
        {
            if (id != habitRecord.Id) return NotFound();

            var original = await VisibleRecords().AsNoTracking().FirstOrDefaultAsync(hr => hr.Id == id);
            if (original == null) return NotFound();

            await ValidateRecordAsync(habitRecord);

            habitRecord.Date = habitRecord.Date.Date;
            if (await _context.HabitRecords.AnyAsync(r => r.Id != id && r.HabitId == habitRecord.HabitId && r.Date == habitRecord.Date))
            {
                ModelState.AddModelError(nameof(HabitRecord.Date), "This habit already has a record for that day.");
            }

            if (!ModelState.IsValid)
            {
                await FillHabitsAsync(habitRecord.HabitId);
                return View(habitRecord);
            }

            try
            {
                _context.Update(habitRecord);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HabitRecordExists(habitRecord.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // The record may have moved to another habit, both need fresh numbers
            await _stats.RefreshStatisticAsync(habitRecord.HabitId);
            if (original.HabitId != habitRecord.HabitId)
            {
                await _stats.RefreshStatisticAsync(original.HabitId);
            }

            NotifySuccess("Record updated.");
            return RedirectToAction(nameof(Index));
        }

        // GET: HabitRecords/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var habitRecord = await VisibleRecords()
                .Include(h => h.Habit)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (habitRecord == null)
            {
                return NotFound();
            }

            return View(habitRecord);
        }

        // POST: HabitRecords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var habitRecord = await VisibleRecords().FirstOrDefaultAsync(r => r.Id == id);
            if (habitRecord != null)
            {
                _context.HabitRecords.Remove(habitRecord);
                await _context.SaveChangesAsync();
                await _stats.RefreshStatisticAsync(habitRecord.HabitId);

                NotifySuccess("The Habit Record has been successfully deleted.");
            }
            return RedirectToAction(nameof(Index));
        }

        private IQueryable<HabitRecord> VisibleRecords()
        {
            if (IsAdmin)
                return _context.HabitRecords;

            var userId = CurrentUserId;
            return _context.HabitRecords.Where(hr => hr.Habit!.UserId == userId);
        }

        private async Task ValidateRecordAsync(HabitRecord habitRecord)
        {
            // Users can only log records for their own habits
            if (!await _stats.VisibleHabits(User).AnyAsync(h => h.Id == habitRecord.HabitId))
            {
                ModelState.AddModelError(nameof(HabitRecord.HabitId), "Please pick one of the habits.");
            }

            if (habitRecord.Date.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(HabitRecord.Date), "The date can't be in the future.");
            }
        }

        private async Task FillHabitsAsync(int? selectedId)
        {
            var habits = await _stats.VisibleHabits(User)
                .Include(h => h.User)
                .OrderBy(h => h.Name)
                .ToListAsync();

            // Admins see every user's habits, so the owner helps tell them apart
            var items = habits.Select(h => new { h.Id, Label = IsAdmin ? $"{h.Name} ({h.User?.Name})" : h.Name });
            ViewData["HabitId"] = new SelectList(items, "Id", "Label", selectedId);
        }

        private bool HabitRecordExists(int id)
        {
            return _context.HabitRecords.Any(e => e.Id == id);
        }
    }
}
