namespace HabitFlow.Blazor.Components.Pages.Habits.Models;

public class ProgressRollingVm
{
    public int HabitId { get; set; }
    public int WindowDays { get; set; }
    public DateOnly Until { get; set; }
    public List<ProgressPointVm> Points { get; set; } = new();
}

public class ProgressPointVm
{
    public DateOnly Date { get; set; }
    public int PlannedDays { get; set; }
    public double SumDailyScore { get; set; }
    public double SuccessRate { get; set; }
}
