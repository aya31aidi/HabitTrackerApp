using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HabitTrackerApp.Models;
using Web.Data;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    public class CategoriesController : AppController
    {
        private readonly WebContext _context;
        private readonly HabitStatsService _stats;

        public CategoriesController(WebContext context, HabitStatsService stats)
        {
            _context = context;
            _stats = stats;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var visibleHabits = _stats.VisibleHabits(User);

            // Counts only include the habits the current account is allowed to see
            var cards = await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryCard(
                    c,
                    visibleHabits.Count(h => h.CategoryId == c.Id),
                    visibleHabits.Count(h => h.CategoryId == c.Id
                        && h.HabitRecords.Any(r => r.IsCompleted && r.Date >= today && r.Date < today.AddDays(1)))))
                .ToListAsync();

            ViewBag.IsAdmin = IsAdmin;
            return View(cards);
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            ViewBag.Habits = await _stats.GetSummariesAsync(User, category.Id);
            ViewBag.IsAdmin = IsAdmin;
            return View(category);
        }

        // GET: Categories/Create
        [Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
        public async Task<IActionResult> Create([Bind("Id,Name")] Category category)
        {
            await ValidateUniqueNameAsync(category);

            if (ModelState.IsValid)
            {
                category.Name = category.Name.Trim();
                _context.Add(category);
                await _context.SaveChangesAsync();

                NotifySuccess($"Category \"{category.Name}\" created.");
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Edit/5
        [Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            await ValidateUniqueNameAsync(category);

            if (ModelState.IsValid)
            {
                try
                {
                    category.Name = category.Name.Trim();
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                NotifySuccess("Category updated.");
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Categories/Delete/5
        [Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.Habits)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                // Habits of this category are removed too (cascade delete)
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                NotifySuccess("Category successfully deleted.");
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateUniqueNameAsync(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
                return;

            var name = category.Name.Trim();
            if (await _context.Categories.AnyAsync(c => c.Name == name && c.Id != category.Id))
            {
                ModelState.AddModelError(nameof(Category.Name), "A category with this name already exists.");
            }
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}
