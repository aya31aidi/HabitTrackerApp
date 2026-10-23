using HabitTrackerApp.Models;

namespace Web.Models
{
    public record DayStatus(DateTime Date, bool IsCompleted, bool HasRecord);

    // Everything the views need to know about one habit's progress
    public class HabitSummary
    {
        public required Habit Habit { get; init; }
        public int TotalCompletions { get; init; }
        public int CurrentStreak { get; init; }
        public int BestStreak { get; init; }
        public double CompletionRate { get; init; }
        public int TrackedDays { get; init; }
        public bool DoneToday { get; init; }
        public DateTime? LastCompleted { get; init; }
        public IReadOnlyList<DayStatus> LastDays { get; init; } = Array.Empty<DayStatus>();
    }
}
