using System.ComponentModel.DataAnnotations;

namespace HabitTrackerApp.Models
{
    public class HabitStatistic
    {
        public int Id { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "The total completions must be a positive number.")]
        [Display(Name = "Total Completions")]
        public int TotalCompletions { get; set; } //= The number of HabitRecords with IsCompleted == true for a specific HabitId

        [Range(0, int.MaxValue, ErrorMessage = "The streak must be a positive number.")]
        public int Streak { get; set; } //number of consecutive days

        [Range(0, 100, ErrorMessage = "The completion rate must be between 0 and 100.")]
        [Display(Name = "Completion Rate")]
        public double CompletionRate { get; set; } //CompletionRate = (TotalCompletions / TotalPossibleDays) * 100

        [Required]
        public int HabitId { get; set; }

        public Habit? Habit { get; set; }
        public HabitStatistic() { }

        public HabitStatistic(int id, int totalCompletions, int streak, double completionRate, int habitId, Habit habit)
        {
            Id = id;
            TotalCompletions = totalCompletions;
            Streak = streak;
            CompletionRate = completionRate;
            HabitId = habitId;
            Habit = habit;
        }
    }
}
