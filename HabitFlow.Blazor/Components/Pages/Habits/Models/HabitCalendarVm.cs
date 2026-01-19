namespace HabitFlow.Blazor.Components.Pages.Habits.Models;

public class HabitCalendarVm
{
    public int HabitId { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<CalendarDayVm> Days { get; set; } = new();
}
