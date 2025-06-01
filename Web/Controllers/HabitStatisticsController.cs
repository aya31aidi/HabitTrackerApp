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
    public class HabitStatisticsController : Controller
    {
        private readonly WebContext _context;

        public HabitStatisticsController(WebContext context)
        {
            _context = context;
        }

        // GET: HabitStatistics
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            ViewBag.Role = role;

            IQueryable<HabitStatistic> statsQuery = _context.HabitStatistics
                .Include(hs => hs.Habit)
                .ThenInclude(h => h.User);

            if (role != "Admin")
            {
                statsQuery = statsQuery.Where(hs => hs.Habit.User.Email == email);
            }

            return View(await statsQuery.ToListAsync());

        }


        // GET: HabitStatistics/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var habitStatistic = await _context.HabitStatistics
             .Include(h => h.Habit)
             .FirstOrDefaultAsync(m => m.Id == id);

            if (habitStatistic == null)
            {
                return NotFound();
            }

            return View(habitStatistic);
        }

        // GET: HabitStatistics/Create
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

        // POST: HabitStatistics/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TotalCompletions,Streak,CompletionRate,HabitId")] HabitStatistic habitStatistic)
        {
            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            if (role != "Admin")
            {
                var habit = await _context.Habits
                    .Include(h => h.User)
                    .FirstOrDefaultAsync(h => h.Id == habitStatistic.HabitId);

                if (habit == null || habit.User.Email != email)
                {
                    return Forbid(); // User trying to create statistic for someone else's habit
                }
            }

            _context.Add(habitStatistic);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: HabitStatistics/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var habitStatistic = await _context.HabitStatistics
                .Include(hs => hs.Habit)
                .ThenInclude(h => h.User)
                .FirstOrDefaultAsync(hs => hs.Id == id);

            if (habitStatistic == null) return NotFound();

            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            if (role != "Admin" && habitStatistic.Habit.User.Email != email)
            {
                return Forbid();
            }

            if (role == "Admin")
            {
                ViewData["HabitId"] = new SelectList(_context.Habits, "Id", "Name", habitStatistic.HabitId);
            }
            else
            {
                var userHabits = _context.Habits.Where(h => h.User.Email == email);
                ViewData["HabitId"] = new SelectList(userHabits, "Id", "Name", habitStatistic.HabitId);
            }

            return View(habitStatistic);
        }

        // POST: HabitStatistics/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TotalCompletions,Streak,CompletionRate,HabitId")] HabitStatistic habitStatistic)
        {
            if (id != habitStatistic.Id) return NotFound();

            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            if (role != "Admin")
            {
                var habit = await _context.Habits.Include(h => h.User).FirstOrDefaultAsync(h => h.Id == habitStatistic.HabitId);
                if (habit == null || habit.User.Email != email)
                {
                    return Forbid();
                }
            }

            try
            {
                _context.Update(habitStatistic);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HabitStatisticExists(habitStatistic.Id))
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
        // GET: HabitStatistics/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var habitStatistic = await _context.HabitStatistics
                .FirstOrDefaultAsync(m => m.Id == id);
            if (habitStatistic == null)
            {
                return NotFound();
            }

            return View(habitStatistic);
        }

        // POST: HabitStatistics/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var habitStatistic = await _context.HabitStatistics.FindAsync(id);
            if (habitStatistic != null)
            {
                _context.HabitStatistics.Remove(habitStatistic);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "The Habit Statistic has been successfully deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool HabitStatisticExists(int id)
        {
            return _context.HabitStatistics.Any(e => e.Id == id);
        }
    }
}
