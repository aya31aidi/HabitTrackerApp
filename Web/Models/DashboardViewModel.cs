using HabitTrackerApp.Models;

namespace Web.Models
{
    public record ChartPoint(string Label, double Value);

    public class DashboardViewModel
    {
        public string DisplayName { get; init; } = string.Empty;
        public bool IsAdmin { get; init; }

        public List<HabitSummary> Habits { get; init; } = new();
        public List<Reminder> UpcomingReminders { get; init; } = new();

        // Completed check-ins per day for the activity chart
        public List<ChartPoint> Activity { get; init; } = new();

        public int CompletedToday => Habits.Count(h => h.DoneToday);
        public int BestStreak => Habits.Count == 0 ? 0 : Habits.Max(h => h.CurrentStreak);
        public double AverageRate => Habits.Count == 0 ? 0 : Math.Round(Habits.Average(h => h.CompletionRate), 1);
        public int TodayPercent => Habits.Count == 0 ? 0 : (int)Math.Round(CompletedToday * 100.0 / Habits.Count);

        // Admin only
        public int UserCount { get; init; }
        public int CategoryCount { get; init; }
    }
}
