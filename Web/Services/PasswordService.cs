using HabitTrackerApp.Models;
using Microsoft.AspNetCore.Identity;

namespace Web.Services
{
    public class PasswordService
    {
        private readonly PasswordHasher<User> _hasher = new();

        public string Hash(User user, string password) => _hasher.HashPassword(user, password);

        /// <summary>
        /// Checks a password against the stored value.
        /// Accounts created before hashing was introduced still hold the raw password,
        /// so those are compared directly and flagged for an upgrade.
        /// </summary>
        public bool Verify(User user, string password, out bool needsRehash)
        {
            needsRehash = false;

            PasswordVerificationResult result;
            try
            {
                result = _hasher.VerifyHashedPassword(user, user.Password, password);
            }
            catch (FormatException)
            {
                // Not a base64 hash at all => legacy plain text value
                result = PasswordVerificationResult.Failed;
            }

            if (result == PasswordVerificationResult.Success)
                return true;

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                needsRehash = true;
                return true;
            }

            if (string.Equals(user.Password, password, StringComparison.Ordinal))
            {
                needsRehash = true;
                return true;
            }

            return false;
        }
    }
}
