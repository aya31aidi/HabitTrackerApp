using System;
using System.ComponentModel.DataAnnotations;

namespace HabitTrackerApp.Models
{
    public class HabitRecord
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "The date is mandatory.")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } // The date on which the habit was tracked : the day the habit was supposed to be completed

        [Display(Name = "Completed")]
        public bool IsCompleted { get; set; }

        [Required]
        [Display(Name = "Habit")]
        public int HabitId { get; set; }

        public Habit? Habit { get; set; }

        public HabitRecord() { }

        public HabitRecord(int id, DateTime date, bool isCompleted, int habitId, Habit habit)
        {
            Id = id;
            Date = date;
            IsCompleted = isCompleted;
            HabitId = habitId;
            Habit = habit;
        }
    }
}
