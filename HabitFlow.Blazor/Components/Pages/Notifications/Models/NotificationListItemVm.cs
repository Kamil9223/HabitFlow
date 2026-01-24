namespace HabitFlow.Blazor.Components.Pages.Notifications.Models;

/// <summary>
/// Model widoku pojedynczego powiadomienia w liście.
/// </summary>
public sealed class NotificationListItemVm
{
    public long Id { get; set; }
    public int HabitId { get; set; }
    public string? HabitTitle { get; set; }
    public DateTimeOffset LocalDate { get; set; }
    public NotificationType Type { get; set; }
    public string TypeLabel { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public AiGenerationStatus? AiStatus { get; set; }
    public string? AiStatusLabel { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
