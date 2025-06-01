using System;
using System.ComponentModel.DataAnnotations;

namespace HabitTrackerApp.Models
{
    public class Reminder
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "The message is required.")]
        [StringLength(200, ErrorMessage = "The message cannot exceed 200 characters.")]
        public string Message { get; set; }

        [Required(ErrorMessage = "Callback time is required.")]
        public DateTime ReminderTime { get; set; }

        [Required]
        public int HabitId { get; set; }

        public Habit Habit { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
        public Reminder() { }

        public Reminder(int id, string message, DateTime reminderTime, int habitId, Habit habit, int userId, User user)
        {
            Id = id;
            Message = message;
            ReminderTime = reminderTime;
            HabitId = habitId;
            Habit = habit;
            UserId = userId;
            User = user;
        }
    }
}
