using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HabitTrackerApp.Models
{
    public class Habit
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "The habit name is required.")]
        [StringLength(100, ErrorMessage = "The name must not exceed 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        [StringLength(255, ErrorMessage = "The objective should not exceed 255 characters.")]
        public string Goal { get; set; }

        [Required(ErrorMessage = "User is required.")]
        public int UserId { get; set; }
        public User User { get; set; }
        public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

        public ICollection<HabitRecord> HabitRecords { get; set; } = new List<HabitRecord>();
        public HabitStatistic HabitStatistic { get; set; }

        public Habit() { }

        public Habit(int id, string name, int categoryId, Category category, string goal, int userId, User user, ICollection<Reminder> reminders , ICollection<HabitRecord> habitRecords, HabitStatistic habitStatistic)
        {
            Id = id;
            Name = name;
            CategoryId = categoryId;
            Category = category;
            Goal = goal;
            UserId = userId;
            User = user;
            Reminders = reminders;
            HabitRecords = habitRecords;
            HabitStatistic = habitStatistic;
        }
    }
}
