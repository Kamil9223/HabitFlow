namespace HabitFlow.Core.Abstractions.Notifications;

/// <summary>
/// Provides notification content generation.
/// </summary>
public interface INotificationContentGenerator
{
    Task<NotificationContentResult> GenerateAsync(
        NotificationContentContext context,
        CancellationToken cancellationToken);
}
