using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Notifications;

/// <summary>
/// Query to retrieve a single notification by ID for the current user.
/// </summary>
public record GetNotificationByIdQuery(long Id) : IQuery<Result<NotificationDetailDto>>;

/// <summary>
/// Data transfer object for a single notification with habit details.
/// </summary>
public record NotificationDetailDto(
    long Id,
    int HabitId,
    string HabitName,
    DateOnly LocalDate,
    NotificationType Type,
    string Content,
    AiGenerationStatus? AiStatus,
    DateTime CreatedAtUtc
);

/// <summary>
/// Handler for retrieving a single notification by ID with ownership validation.
/// </summary>
public class GetNotificationByIdQueryHandler(
    HabitFlowDbContext context,
    ILoggedUserContext loggedUserContext)
    : IQueryHandler<GetNotificationByIdQuery, Result<NotificationDetailDto>>
{
    public async Task<Result<NotificationDetailDto>> Handle(
        GetNotificationByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Id <= 0)
            return Result.Failure<NotificationDetailDto>(
                Error.NotFound("NOTIFICATION_NOT_FOUND", "Notification not found"));

        var user = loggedUserContext.GetUser();

        var notification = await context.Notifications
            .AsNoTracking()
            .Where(n => n.Id == query.Id && n.UserId == user.UserId)
            .Select(n => new NotificationDetailDto(
                n.Id,
                n.HabitId,
                n.Habit.Title,
                n.LocalDate,
                n.Type,
                n.Content,
                n.AiStatus,
                n.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return notification is not null
            ? Result.Success(notification)
            : Result.Failure<NotificationDetailDto>(
                Error.NotFound("NOTIFICATION_NOT_FOUND", "Notification not found"));
    }
}
