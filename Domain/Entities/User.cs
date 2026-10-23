using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HabitTrackerApp.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "The name is required.")]
        [StringLength(100, ErrorMessage = "The name must not exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        // Holds the password hash, not the raw password (length rules live in the view models)
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(256)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Member Since")]
        public DateTime CreatedAt { get; set; }

        public ICollection<Habit> Habits { get; set; } = new List<Habit>();
        public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();


        public User() { }

        public User(int id, string name, string email, string password, DateTime createdAt, ICollection<Habit> habits , ICollection<Reminder> reminders)
        {
            Id = id;
            Name = name;
            Email = email;
            Password = password;
            CreatedAt = createdAt;
            Habits = habits;
            Reminders = reminders;
        }
    }
}
