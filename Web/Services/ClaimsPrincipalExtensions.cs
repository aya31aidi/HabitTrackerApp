using System.Security.Claims;

namespace Web.Services
{
    public static class ClaimsPrincipalExtensions
    {
        public const string AdminRole = "Admin";
        public const string UserRole = "User";

        public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole(AdminRole);

        // The built-in admin is not stored in the Users table, so it gets 0
        public static int GetUserId(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;
        }

        public static string GetEmail(this ClaimsPrincipal principal) =>
            principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        public static string GetDisplayName(this ClaimsPrincipal principal) =>
            principal.FindFirstValue(ClaimTypes.Name) ?? principal.GetEmail();

        public static string GetInitials(this ClaimsPrincipal principal) => Initials(principal.GetDisplayName());

        public static string Initials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            var parts = name.Split(new[] { ' ', '.', '_', '-', '@' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();

            return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
        }
    }
}
