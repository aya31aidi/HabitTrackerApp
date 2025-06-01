using System.Data.Entity;
using HabitTrackerApp.Models;

public class HabitTrackerContext : DbContext
{
    public DbSet<Category> Categories { get; set; }
    public DbSet<Habit> Habits { get; set; }
    public DbSet<HabitRecord> HabitRecords { get; set; }
    public DbSet<HabitStatistic> HabitStatistics { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<User> Users { get; set; }

    public HabitTrackerContext() : base("Data Source=DESKTOP-IQONVA5;Initial Catalog=HabitTrackerDb;Integrated Security=True;")
    {
    }
  
}
