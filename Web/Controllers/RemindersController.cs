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
    public class RemindersController : Controller
    {
        private readonly WebContext _context;

        public RemindersController(WebContext context)
        {
            _context = context;
        }

        // GET: Reminders
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            ViewBag.Role = role;
            ViewBag.Email = email;

            IQueryable<Reminder> reminders = _context.Reminders.Include(r => r.Habit)
                .Include(r => r.User);

            if (role != "Admin")
            {
                reminders = reminders.Where(r => r.User.Email == email);
            }

            return View(await reminders.ToListAsync());
        }

        // GET: Reminders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reminder = await _context.Reminders
                .Include(r => r.Habit)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reminder == null)
            {
                return NotFound();
            }

            return View(reminder);
        }

        // GET: Reminders/Create
        public IActionResult Create()
        {
            ViewData["HabitId"] = new SelectList(_context.Habits, "Id", "Goal");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        // POST: Reminders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Message,ReminderTime,HabitId,UserId")] Reminder reminder)
        {
            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            if (role != "Admin")
            {
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (currentUser != null)
                {
                    reminder.UserId = currentUser.Id;
                }
            }

            _context.Add(reminder);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Reminders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reminder = await _context.Reminders.FindAsync(id);
            if (reminder == null)
            {
                return NotFound();
            }
            ViewData["HabitId"] = new SelectList(_context.Habits, "Id", "Goal", reminder.HabitId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", reminder.UserId);
            return View(reminder);
        }

        // POST: Reminders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Message,ReminderTime,HabitId,UserId")] Reminder reminder)
        {
            if (id != reminder.Id)
            {
                return NotFound();
            }

            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            if (role != "Admin")
            {
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (currentUser != null)
                {
                    reminder.UserId = currentUser.Id;
                }
            }

            try
            {
                _context.Update(reminder);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reminders.Any(r => r.Id == reminder.Id))
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

        // GET: Reminders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reminder = await _context.Reminders
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
            var reminder = await _context.Reminders.FindAsync(id);
            if (reminder != null)
            {
                _context.Reminders.Remove(reminder);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "The reminder has been successfully deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ReminderExists(int id)
        {
            return _context.Reminders.Any(e => e.Id == id);
        }
    }
}
