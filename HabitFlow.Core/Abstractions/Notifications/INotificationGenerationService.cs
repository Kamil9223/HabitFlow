namespace HabitFlow.Core.Abstractions.Notifications;

/// <summary>
/// Orchestrates miss-due notification generation.
/// </summary>
public interface INotificationGenerationService
{
    Task<NotificationGenerationSummary> GenerateNotificationsAsync(CancellationToken cancellationToken);
}
