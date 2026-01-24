using HabitFlow.Core.Abstractions.Notifications;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Infrastructure.Notifications;

/// <summary>
/// EF Core repository for notifications.
/// </summary>
public sealed class NotificationRepository(HabitFlowDbContext context) : INotificationRepository
{
    public Task<bool> ExistsAsync(
        Guid userId,
        int habitId,
        DateOnly localDate,
        NotificationType type,
        CancellationToken cancellationToken)
        => context.Notifications
            .AsNoTracking()
            .AnyAsync(n =>
                    n.UserId == userId &&
                    n.HabitId == habitId &&
                    n.LocalDate == localDate &&
                    n.Type == type,
                cancellationToken);

    public async Task CreateAsync(Notification notification, CancellationToken cancellationToken)
    {
        context.Notifications.Add(notification);
        await context.SaveChangesAsync(cancellationToken);
    }
}
