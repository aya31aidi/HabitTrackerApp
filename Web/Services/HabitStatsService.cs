using System.Security.Claims;
using HabitTrackerApp.Models;
using Microsoft.EntityFrameworkCore;
using Web.Data;
using Web.Models;

namespace Web.Services
{
    /// <summary>
    /// Statistics are derived from the habit records instead of being typed by hand.
    /// The HabitStatistics table is still kept in sync so the desktop app keeps working.
    /// </summary>
    public class HabitStatsService
    {
        public const int RecentDays = 7;

        private readonly WebContext _context;

        public HabitStatsService(WebContext context)
        {
            _context = context;
        }

        public IQueryable<Habit> VisibleHabits(ClaimsPrincipal user)
        {
            IQueryable<Habit> habits = _context.Habits;
            if (!user.IsAdmin())
            {
                var userId = user.GetUserId();
                habits = habits.Where(h => h.UserId == userId);
            }
            return habits;
        }

        public async Task<List<HabitSummary>> GetSummariesAsync(ClaimsPrincipal user, int? categoryId = null)
        {
            var query = VisibleHabits(user)
                .Include(h => h.Category)
                .Include(h => h.User)
                .Include(h => h.HabitRecords)
                .AsNoTracking();

            if (categoryId.HasValue)
                query = query.Where(h => h.CategoryId == categoryId.Value);

            var habits = await query.OrderBy(h => h.Name).ToListAsync();
            return habits.Select(h => Summarize(h, h.HabitRecords, DateTime.Today)).ToList();
        }

        public static HabitSummary Summarize(Habit habit, IEnumerable<HabitRecord> records, DateTime today)
        {
            // One entry per day, a completed record wins over a missed one
            var byDay = records
                .Where(r => r.Date.Date <= today)
                .GroupBy(r => r.Date.Date)
                .ToDictionary(g => g.Key, g => g.Any(r => r.IsCompleted));

            var completedDays = byDay.Where(d => d.Value).Select(d => d.Key).OrderBy(d => d).ToList();

            var trackedDays = byDay.Count == 0 ? 0 : (today - byDay.Keys.Min()).Days + 1;
            var rate = trackedDays == 0 ? 0 : Math.Round(completedDays.Count * 100.0 / trackedDays, 1);

            var lastDays = Enumerable.Range(0, RecentDays)
                .Select(offset => today.AddDays(offset - RecentDays + 1))
                .Select(day => new DayStatus(day, byDay.TryGetValue(day, out var done) && done, byDay.ContainsKey(day)))
                .ToList();

            return new HabitSummary
            {
                Habit = habit,
                TotalCompletions = completedDays.Count,
                CurrentStreak = CurrentStreak(completedDays, today),
                BestStreak = BestStreak(completedDays),
                CompletionRate = Math.Min(rate, 100),
                TrackedDays = trackedDays,
                DoneToday = completedDays.Contains(today),
                LastCompleted = completedDays.Count > 0 ? completedDays[^1] : null,
                LastDays = lastDays
            };
        }

        // A streak stays alive until the end of today, so it may start from yesterday
        private static int CurrentStreak(List<DateTime> completedDays, DateTime today)
        {
            var set = completedDays.ToHashSet();
            var day = set.Contains(today) ? today : today.AddDays(-1);
            var streak = 0;
            while (set.Contains(day))
            {
                streak++;
                day = day.AddDays(-1);
            }
            return streak;
        }

        private static int BestStreak(List<DateTime> sortedDays)
        {
            int best = 0, current = 0;
            DateTime? previous = null;
            foreach (var day in sortedDays)
            {
                current = previous.HasValue && (day - previous.Value).Days == 1 ? current + 1 : 1;
                best = Math.Max(best, current);
                previous = day;
            }
            return best;
        }

        /// <summary>
        /// Flips the completion of a habit for the given day and returns the new state.
        /// </summary>
        public async Task<bool> ToggleAsync(int habitId, DateTime date)
        {
            var day = date.Date;
            var records = await _context.HabitRecords
                .Where(r => r.HabitId == habitId && r.Date >= day && r.Date < day.AddDays(1))
                .ToListAsync();

            bool completed;
            if (records.Count == 0)
            {
                _context.HabitRecords.Add(new HabitRecord { HabitId = habitId, Date = day, IsCompleted = true });
                completed = true;
            }
            else
            {
                completed = !records.Any(r => r.IsCompleted);
                records[0].IsCompleted = completed;

                // Clean up duplicates left over from the old manual forms
                _context.HabitRecords.RemoveRange(records.Skip(1));
            }

            await _context.SaveChangesAsync();
            await RefreshStatisticAsync(habitId);
            return completed;
        }

        public async Task RefreshStatisticAsync(int habitId)
        {
            var habit = await _context.Habits
                .Include(h => h.HabitRecords)
                .FirstOrDefaultAsync(h => h.Id == habitId);
            if (habit == null)
                return;

            var summary = Summarize(habit, habit.HabitRecords, DateTime.Today);

            var statistic = await _context.HabitStatistics.FirstOrDefaultAsync(s => s.HabitId == habitId);
            if (statistic == null)
            {
                statistic = new HabitStatistic { HabitId = habitId };
                _context.HabitStatistics.Add(statistic);
            }

            statistic.TotalCompletions = summary.TotalCompletions;
            statistic.Streak = summary.CurrentStreak;
            statistic.CompletionRate = summary.CompletionRate;

            await _context.SaveChangesAsync();
        }

        public async Task RefreshAllAsync(ClaimsPrincipal user)
        {
            var ids = await VisibleHabits(user).Select(h => h.Id).ToListAsync();
            foreach (var id in ids)
                await RefreshStatisticAsync(id);
        }
    }
}
