using HabitTrackerApp.Models;

namespace Web.Models
{
    public record CategoryCard(Category Category, int HabitCount, int DoneToday);
}
