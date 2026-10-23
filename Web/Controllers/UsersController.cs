using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using HabitTrackerApp.Models;
using Web.Data;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    [Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
    public class UsersController : AppController
    {
        private readonly WebContext _context;
        private readonly PasswordService _passwords;
        private readonly AdminOptions _admin;

        public UsersController(WebContext context, PasswordService passwords, IOptions<AdminOptions> admin)
        {
            _context = context;
            _passwords = passwords;
            _admin = admin.Value;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .OrderBy(u => u.Name)
                .Select(u => new UserRow(
                    u.Id,
                    u.Name,
                    u.Email,
                    u.CreatedAt,
                    u.Habits.Count,
                    u.Reminders.Count,
                    u.Habits.SelectMany(h => h.HabitRecords).Max(r => (DateTime?)r.Date)))
                .ToListAsync();

            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Habits)
                    .ThenInclude(h => h.Category)
                .Include(u => u.Habits)
                    .ThenInclude(h => h.HabitRecords)
                .Include(u => u.Reminders)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.Habits = user.Habits
                .Select(h => HabitStatsService.Summarize(h, h.HabitRecords, DateTime.Today))
                .OrderByDescending(s => s.CompletionRate)
                .ToList();

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View(new UserFormViewModel());
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "Password is required.");
            }

            await ValidateEmailAsync(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new User
            {
                Name = model.Name.Trim(),
                Email = model.Email.Trim(),
                CreatedAt = DateTime.Now
            };
            user.Password = _passwords.Hash(user, model.Password!);

            _context.Add(user);
            await _context.SaveChangesAsync();

            NotifySuccess($"Account created for {user.Name}.");
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(new UserFormViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            });
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserFormViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            await ValidateEmailAsync(model);

            if (!ModelState.IsValid)
            {
                model.CreatedAt = user.CreatedAt;
                return View(model);
            }

            user.Name = model.Name.Trim();
            user.Email = model.Email.Trim();

            // If a new password is filled, we replace it, otherwise the old one stays
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                user.Password = _passwords.Hash(user, model.Password);
            }

            await _context.SaveChangesAsync();

            NotifySuccess("User updated.");
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Habits)
                .Include(u => u.Reminders)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users
                .Include(u => u.Reminders)
                .Include(u => u.Habits)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user != null)
            {
                // The foreign keys to Users are RESTRICT, so dependents must go first.
                // Removing a habit also cascades to its records and statistic in the database.
                _context.Reminders.RemoveRange(user.Reminders);
                _context.Habits.RemoveRange(user.Habits);
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                NotifySuccess("The user has been successfully deleted.");
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateEmailAsync(UserFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
                return;

            var email = model.Email.Trim();
            var taken = string.Equals(email, _admin.Email, StringComparison.OrdinalIgnoreCase)
                || await _context.Users.AnyAsync(u => u.Email == email && u.Id != model.Id);

            if (taken)
            {
                ModelState.AddModelError(nameof(model.Email), "Email is already taken.");
            }
        }
    }
}
