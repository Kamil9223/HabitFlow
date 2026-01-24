namespace HabitFlow.Blazor.Components.Pages.Notifications.Models;

/// <summary>
/// Typ powiadomienia (zsynchronizowany z HabitFlow.Data.Enums.NotificationType).
/// </summary>
public enum NotificationType : byte
{
    /// <summary>
    /// Powiadomienie wyzwolone gdy pominięto zaplanowany dzień.
    /// </summary>
    MissDue = 1
}
