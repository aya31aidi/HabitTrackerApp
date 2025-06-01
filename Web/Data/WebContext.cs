using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HabitTrackerApp.Models;

namespace Web.Data
{
    public class WebContext : DbContext
    {
        public WebContext(DbContextOptions<WebContext> options)
            : base(options)
        {

        }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Habit> Habits { get; set; }
        public DbSet<HabitRecord> HabitRecords { get; set; }
        public DbSet<HabitStatistic> HabitStatistics { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<User> Users { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // HABIT -> USER : ON DELETE RESTRICT
            modelBuilder.Entity<Habit>()
                .HasOne(h => h.User)
                .WithMany(u => u.Habits)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade); // CASCADE DELETE HERE : User deleted => its Habits deleted

            // REMINDER -> USER : ON DELETE RESTRICT
            modelBuilder.Entity<Reminder>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reminders)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade); // CASCADE DELETE HERE : User deleted => its Reminders deleted

            // REMINDER -> HABIT : ON DELETE CASCADE (OK)
            modelBuilder.Entity<Reminder>()
                .HasOne(r => r.Habit)
                .WithMany(h => h.Reminders)
                .HasForeignKey(r => r.HabitId)
                .OnDelete(DeleteBehavior.Cascade); // A habit deleted => reminders deleted
        }

    }
}
