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
        public DbSet<Category> Categories { get; set; } = default!;
        public DbSet<Habit> Habits { get; set; } = default!;
        public DbSet<HabitRecord> HabitRecords { get; set; } = default!;
        public DbSet<HabitStatistic> HabitStatistics { get; set; } = default!;
        public DbSet<Reminder> Reminders { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // HABIT -> USER : ON DELETE RESTRICT
            // SQL Server refuses two cascade paths from Users to Reminders (direct + through Habits),
            // so the user's habits and reminders are removed explicitly in UsersController.
            modelBuilder.Entity<Habit>()
                .HasOne(h => h.User)
                .WithMany(u => u.Habits)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // REMINDER -> USER : ON DELETE RESTRICT
            modelBuilder.Entity<Reminder>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reminders)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // REMINDER -> HABIT : ON DELETE CASCADE (OK)
            modelBuilder.Entity<Reminder>()
                .HasOne(r => r.Habit)
                .WithMany(h => h.Reminders)
                .HasForeignKey(r => r.HabitId)
                .OnDelete(DeleteBehavior.Cascade); // A habit deleted => reminders deleted

            // Lookups by habit + day happen on every check-in
            modelBuilder.Entity<HabitRecord>()
                .HasIndex(r => new { r.HabitId, r.Date });
        }

    }
}
