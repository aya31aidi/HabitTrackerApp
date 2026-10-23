using System.Security.Claims;
using HabitTrackerApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Web.Data;
using Web.Services;

namespace Web.Controllers
{
    public class AccountController : AppController
    {
        private readonly WebContext _context;
        private readonly PasswordService _passwords;
        private readonly AdminOptions _admin;

        public AccountController(WebContext context, PasswordService passwords, IOptions<AdminOptions> admin)
        {
            _context = context;
            _passwords = passwords;
            _admin = admin.Value;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim();

            // Admin credentials come from appsettings.json
            if (string.Equals(email, _admin.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (model.Password == _admin.Password)
                {
                    await SignInAsync(0, _admin.Name, _admin.Email, ClaimsPrincipalExtensions.AdminRole, model.RememberMe);
                    return RedirectAfterLogin(returnUrl, _admin.Name);
                }
            }
            else
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null && _passwords.Verify(user, model.Password, out var needsRehash))
                {
                    if (needsRehash)
                    {
                        user.Password = _passwords.Hash(user, model.Password);
                        await _context.SaveChangesAsync();
                    }

                    await SignInAsync(user.Id, user.Name, user.Email, ClaimsPrincipalExtensions.UserRole, model.RememberMe);
                    return RedirectAfterLogin(returnUrl, user.Name);
                }
            }

            // Same message in both cases so nobody can probe which emails exist
            ModelState.AddModelError(string.Empty, "Incorrect email or password.");
            return View(model);
        }

        // Register Action (GET)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // Register Action (POST)
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim();

            // Check if the email already exists (or belongs to the admin account)
            var emailTaken = string.Equals(email, _admin.Email, StringComparison.OrdinalIgnoreCase)
                || await _context.Users.AnyAsync(u => u.Email == email);
            if (emailTaken)
            {
                ModelState.AddModelError(nameof(model.Email), "Email is already taken.");
                return View(model);
            }

            var newUser = new User
            {
                Name = model.Username.Trim(),
                Email = email,
                CreatedAt = DateTime.Now
            };
            newUser.Password = _passwords.Hash(newUser, model.Password);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Sign the new account in right away instead of sending it back to the login form
            await SignInAsync(newUser.Id, newUser.Name, newUser.Email, ClaimsPrincipalExtensions.UserRole, false);
            NotifySuccess($"Welcome aboard, {newUser.Name}! Start by creating a first habit.");
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            NotifySuccess("Signed out. See you soon!");
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        private async Task SignInAsync(int id, string name, string email, string role, bool persistent)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, id.ToString()),
                new(ClaimTypes.Name, name),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = persistent });
        }

        private IActionResult RedirectAfterLogin(string? returnUrl, string name)
        {
            NotifySuccess($"Welcome back, {name}!");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
    }

}
