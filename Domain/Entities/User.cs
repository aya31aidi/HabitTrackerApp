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
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "The password must contain between 6 and 50 characters.")]
        public string Password { get; set; }

        [DataType(DataType.Date)]
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
