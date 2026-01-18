namespace HabitFlow.Blazor.Components.Pages.Habits.Models;

public sealed class ConfirmDialogOptions
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ConfirmButtonText { get; set; } = "Potwierdź";
    public string CancelButtonText { get; set; } = "Anuluj";
}
