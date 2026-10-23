using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    // Used by the admin forms so the stored password hash never travels to the browser
    public class UserFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "The name is required.")]
        [StringLength(100, ErrorMessage = "The name must not exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        // Required on create, optional on edit (blank = keep the current one)
        [StringLength(50, MinimumLength = 6, ErrorMessage = "The password must contain between 6 and 50 characters.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Display(Name = "Member Since")]
        public DateTime CreatedAt { get; set; }
    }

    public record UserRow(int Id, string Name, string Email, DateTime CreatedAt, int HabitCount, int ReminderCount, DateTime? LastActivity);
}
