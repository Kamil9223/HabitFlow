using HabitFlow.Client;

namespace HabitFlow.Blazor.Components.Pages.Habits.Models;

public sealed class CheckinTargetVm
{
    public int HabitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public HabitType Type { get; set; }
    public CompletionMode CompletionMode { get; set; }
    public int TargetValue { get; set; }
    public string? TargetUnit { get; set; }
}
