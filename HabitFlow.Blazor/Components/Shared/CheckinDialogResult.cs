namespace HabitFlow.Blazor.Components.Shared;

public sealed class CheckinDialogResult
{
    public int HabitId { get; set; }
    public int ActualValue { get; set; }
    public DateOnly SelectedDate { get; set; }
}
