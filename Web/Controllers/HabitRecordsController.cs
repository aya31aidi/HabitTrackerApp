using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HabitTrackerApp.Models;
using Web.Data;

namespace Web.Controllers
{
    public class HabitRecordsController : Controller
    {
        private readonly WebContext _context;

        public HabitRecordsController(WebContext context)
        {
            _context = context;
        }

        // GET: HabitRecords
        public async Task<IActionResult> Index()
        {
            
            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            ViewBag.Role = role;

            IQueryable<HabitRecord> habitRecordsQuery = _context.HabitRecords
                .Include(hr => hr.Habit)
                    .ThenInclude(h => h.User) as IQueryable<HabitRecord>;

            if (role != "Admin")
            {
                habitRecordsQuery = habitRecordsQuery
                    .Where(hr => hr.Habit.User.Email == email);
            }

            return View(await habitRecordsQuery.ToListAsync());
        }

        // GET: HabitRecords/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var habitRecord = await _context.HabitRecords
                .Include(h => h.Habit)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (habitRecord == null)
            {
                return NotFound();
            }

            return View(habitRecord);
        }

        // GET: HabitRecords/Create
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            ViewBag.Role = role;

            if (role == "Admin")
            {
                ViewData["HabitId"] = new SelectList(_context.Habits.Include(h => h.User), "Id", "Name");
            }
            else
            {
                var userHabits = _context.Habits
                    .Include(h => h.User)
                    .Where(h => h.User.Email == email);

                ViewData["HabitId"] = new SelectList(userHabits, "Id", "Name");
            }

            return View();
        }

        // POST: HabitRecords/Create
      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Date,IsCompleted,HabitId")] HabitRecord habitRecord)
        {
            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            if (role != "Admin")
            {
                var habit = await _context.Habits
                    .Include(h => h.User)
                    .FirstOrDefaultAsync(h => h.Id == habitRecord.HabitId);

                if (habit == null || habit.User.Email != email)
                {
                    return Forbid(); // User trying to create record for someone else's habit
                }
            }

            _context.Add(habitRecord);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: HabitRecords/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var habitRecord = await _context.HabitRecords
                .Include(hr => hr.Habit)
                .ThenInclude(h => h.User)
                .FirstOrDefaultAsync(hr => hr.Id == id);

            if (habitRecord == null) return NotFound();

            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            if (role != "Admin" && habitRecord.Habit.User.Email != email)
            {
                return Forbid();
            }

            ViewBag.Role = role;

            if (role == "Admin")
            {
                ViewData["HabitId"] = new SelectList(_context.Habits, "Id", "Name", habitRecord.HabitId);
            }
            else
            {
                var userHabits = _context.Habits
                    .Where(h => h.User.Email == email);
                ViewData["HabitId"] = new SelectList(userHabits, "Id", "Name", habitRecord.HabitId);
            }

            return View(habitRecord);
        }

        // POST: HabitRecords/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Date,IsCompleted,HabitId")] HabitRecord habitRecord)
        {
            if (id != habitRecord.Id) return NotFound();

            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            if (role != "Admin")
            {
                var habit = await _context.Habits
                    .Include(h => h.User)
                    .FirstOrDefaultAsync(h => h.Id == habitRecord.HabitId);

                if (habit == null || habit.User.Email != email)
                {
                    return Forbid();
                }
            }

            try
            {
                _context.Update(habitRecord);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.HabitRecords.Any(hr => hr.Id == habitRecord.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: HabitRecords/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var habitRecord = await _context.HabitRecords
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
            var habitRecord = await _context.HabitRecords.FindAsync(id);
            if (habitRecord != null)
            {
                _context.HabitRecords.Remove(habitRecord);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "The Habit Record has been successfully deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
        private bool HabitRecordExists(int id)
        {
            return _context.HabitRecords.Any(e => e.Id == id);
        }
    }
}
