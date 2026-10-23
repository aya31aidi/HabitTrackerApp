using HabitTrackerApp.Models;

namespace Web.Models
{
    public class HabitListViewModel
    {
        public List<HabitSummary> Habits { get; init; } = new();
        public List<Category> Categories { get; init; } = new();
        public int? SelectedCategoryId { get; init; }
        public bool IsAdmin { get; init; }
    }

    public class HabitDetailsViewModel
    {
        public required HabitSummary Summary { get; init; }

        // Oldest day first, used to draw the calendar heatmap
        public List<DayStatus> Calendar { get; init; } = new();
        public List<HabitRecord> RecentRecords { get; init; } = new();
        public List<Reminder> Reminders { get; init; } = new();

        public Habit Habit => Summary.Habit;
    }
}
