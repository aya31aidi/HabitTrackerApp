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
    public class HabitsController : Controller
    {
        private readonly WebContext _context;

        public HabitsController(WebContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get Role and Email from session
            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            ViewBag.Role = role;
            ViewBag.Email = email;

            IQueryable<Habit> webContext = _context.Habits
                .Include(h => h.Category)
                .Include(h => h.User);

            if (role != "Admin")
            {
                webContext = webContext.Where(h => h.User.Email == email);
            }

            return View(await webContext.ToListAsync());
        }

        public async Task<IActionResult> ByCategory(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            IQueryable<Habit> habits = _context.Habits
                .Include(h => h.Category)
                .Include(h => h.User)
                .Where(h => h.CategoryId == id);

            if (role != "Admin")
            {
                habits = habits.Where(h => h.User.Email == email);
            }

            ViewBag.CategoryName = _context.Categories.FirstOrDefault(c => c.Id == id)?.Name;
            ViewBag.Role = role;
            ViewBag.Email = email;

            return View("Index", await habits.ToListAsync());
        }

        // GET: Habits/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var habit = await _context.Habits
                .Include(h => h.Category)
                .Include(h => h.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (habit == null)
            {
                return NotFound();
            }

            return View(habit);
        }

        // GET: Habits/Create
        public IActionResult Create()
        {
           
            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            ViewBag.Role = role;

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");

            if (role == "Admin")
            {
                // Admin selects user
                ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email");
            }
            return View();
        }

        // POST: Habits/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,CategoryId,Goal,UserId")] Habit habit)
        {

            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            if (role != "Admin")
            {
                // Get current user ID by email
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (currentUser != null)
                {
                    habit.UserId = currentUser.Id;
                }
            }
                _context.Add(habit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
        }

        // GET: Habits/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            var habit = await _context.Habits.FindAsync(id);
            if (habit == null)
            {
                return NotFound();
            }

            var role = HttpContext.Session.GetString("Role");
            ViewBag.Role = role;

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", habit.CategoryId);

            if (role == "Admin")
            {
                ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", habit.UserId);
            }

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

            var role = HttpContext.Session.GetString("Role");
            var email = HttpContext.Session.GetString("Email");

            if (role != "Admin")
            {
                // Force UserId to be the one from session
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (currentUser != null)
                {
                    habit.UserId = currentUser.Id;
                }
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
                return RedirectToAction(nameof(Index));

        }

        // GET: Habits/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var habit = await _context.Habits
                .Include(h => h.Category)
                .Include(h => h.User)
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
            var habit = await _context.Habits.FindAsync(id);
            if (habit != null)
            {
                _context.Habits.Remove(habit);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "The habit has been successfully deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool HabitExists(int id)
        {
            return _context.Habits.Any(e => e.Id == id);
        }
    }
}
