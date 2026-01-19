namespace HabitFlow.Blazor.Components.Pages.Habits.Models;

public class HabitDetailsState
{
    public bool IsLoading { get; set; }
    public bool IsLoadingCalendar { get; set; }
    public bool IsLoadingProgress { get; set; }
    public string? ErrorMessage { get; set; }
    public HabitDetailsVm? Habit { get; set; }
    public HabitCalendarVm? Calendar { get; set; }
    public ProgressRollingVm? Progress { get; set; }
    public int WindowDays { get; set; } = 7;
    public int ActiveTab { get; set; } = 0;
}
