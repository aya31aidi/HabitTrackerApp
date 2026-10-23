namespace Web.Models
{
    public class StatisticsViewModel
    {
        public List<HabitSummary> Habits { get; init; } = new();
        public List<ChartPoint> Trend { get; init; } = new();
        public List<ChartPoint> ByCategory { get; init; } = new();
        public List<ChartPoint> ByWeekday { get; init; } = new();
        public int TrendDays { get; init; }

        public int TotalCompletions => Habits.Sum(h => h.TotalCompletions);
        public int BestStreak => Habits.Count == 0 ? 0 : Habits.Max(h => h.BestStreak);
        public double AverageRate => Habits.Count == 0 ? 0 : Math.Round(Habits.Average(h => h.CompletionRate), 1);
        public HabitSummary? TopHabit => Habits.OrderByDescending(h => h.CompletionRate).ThenByDescending(h => h.TotalCompletions).FirstOrDefault();
    }
}
