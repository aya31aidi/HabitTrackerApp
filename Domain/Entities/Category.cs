using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HabitTrackerApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "The category name is required.")]
        [StringLength(100, ErrorMessage = "The name must not exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        public ICollection<Habit> Habits { get; set; } = new List<Habit>();

        public Category() { }

        public Category(int id, string name, ICollection<Habit> habits)
        {
            Id = id;
            Name = name;
            Habits = habits;
        }
    }
}
