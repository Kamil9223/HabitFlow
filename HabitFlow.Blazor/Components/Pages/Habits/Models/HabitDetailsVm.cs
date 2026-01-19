using HabitFlow.Client;

namespace HabitFlow.Blazor.Components.Pages.Habits.Models;

public class HabitDetailsVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public HabitType Type { get; set; }
    public CompletionMode CompletionMode { get; set; }
    public int DaysOfWeekMask { get; set; }
    public string ScheduleLabel { get; set; } = string.Empty;
    public int TargetValue { get; set; }
    public string? TargetUnit { get; set; }
    public DateOnly? DeadlineDate { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? SuccessRate { get; set; }
}
