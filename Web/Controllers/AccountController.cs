using HabitTrackerApp.Models;
using System;
using Microsoft.AspNetCore.Mvc;
using Web.Data;

namespace Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly WebContext _context;

        // Admin credentials hardcoded
        private const string AdminEmail = "admin@habittracker.com";
        private const string AdminPassword = "Admin123";

        public AccountController(WebContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.Role == "Admin")
            {
                if (model.Email == AdminEmail && model.Password == AdminPassword)
                {
                    HttpContext.Session.SetString("Role", "Admin");
                    HttpContext.Session.SetString("Email", model.Email);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["ErrorMessage"] = "Admin credentials incorrect.";
                    return RedirectToAction("Login");
                }
            }
            else if (model.Role == "User")
            {
                var user = _context.Users
                    .FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);

                if (user != null)
                {
                    HttpContext.Session.SetString("Role", "User");
                    HttpContext.Session.SetString("Email", user.Email);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["ErrorMessage"] = "User credentials incorrect.";
                    return RedirectToAction("Login");
                }
            }

            ModelState.AddModelError("", "Invalid role selection");
            return View(model);
        }

        // Register Action (GET)
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        // Register Action (POST)
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
            {
            if (ModelState.IsValid)
            {
                // Check if the email already exists
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email is already taken.");
                    return View(model);
                }

                // Create and save new user
                var newUser = new User
                {
                    Name = model.Username,
                    Email = model.Email,
                    Password = model.Password,
                    CreatedAt = DateTime.Now // Automatically set the current date and time
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Registration successful! You can now login.";

                // Redirect to the Login Page
                return RedirectToAction("Login", "Account");    
            }

            return View(model);
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Clear all session data
            return RedirectToAction("Login", "Account"); // Redirect to Login page
        }
    }

}
