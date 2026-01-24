using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;

namespace HabitFlow.Core.Abstractions.Notifications;

/// <summary>
/// Repository for notification persistence.
/// </summary>
public interface INotificationRepository
{
    Task<bool> ExistsAsync(
        Guid userId,
        int habitId,
        DateOnly localDate,
        NotificationType type,
        CancellationToken cancellationToken);

    Task CreateAsync(Notification notification, CancellationToken cancellationToken);
}
