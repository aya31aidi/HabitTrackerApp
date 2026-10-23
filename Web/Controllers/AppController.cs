using Microsoft.AspNetCore.Mvc;
using Web.Services;

namespace Web.Controllers
{
    /// <summary>
    /// Shared helpers for controllers that work with the signed-in account.
    /// </summary>
    public abstract class AppController : Controller
    {
        protected bool IsAdmin => User.IsAdmin();

        protected int CurrentUserId => User.GetUserId();

        // Picked up by the toast area in _Layout
        protected void NotifySuccess(string message) => TempData["SuccessMessage"] = message;

        protected void NotifyError(string message) => TempData["ErrorMessage"] = message;

        protected bool IsAjaxRequest() =>
            Request.Headers.XRequestedWith == "XMLHttpRequest";
    }
}
