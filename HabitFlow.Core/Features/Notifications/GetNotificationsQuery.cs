using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Notifications;

/// <summary>
/// Supported fields for sorting notifications.
/// </summary>
public enum NotificationSortField
{
    CreatedAtUtc,
    LocalDate,
    Type
}

/// <summary>
/// Query to retrieve a paginated list of notifications for the current user.
/// </summary>
public record GetNotificationsQuery(
    int Page = 1,
    int PageSize = 20,
    NotificationSortField SortField = NotificationSortField.CreatedAtUtc,
    SortDirection SortDirection = SortDirection.Desc
) : IQuery<Result<PagedNotificationsDto>>;

/// <summary>
/// Data transfer object for paginated notifications list.
/// </summary>
public record PagedNotificationsDto(
    int TotalCount,
    IReadOnlyList<NotificationDto> Items
);

/// <summary>
/// Data transfer object for a single notification.
/// </summary>
public record NotificationDto(
    long Id,
    int HabitId,
    DateOnly LocalDate,
    NotificationType Type,
    string Content,
    AiGenerationStatus? AiStatus,
    DateTime CreatedAtUtc
);

/// <summary>
/// Handler for retrieving a paginated and sorted list of notifications for the current user.
/// </summary>
public class GetNotificationsQueryHandler(
    HabitFlowDbContext context,
    ILoggedUserContext loggedUserContext)
    : IQueryHandler<GetNotificationsQuery, Result<PagedNotificationsDto>>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;
    private const int MinPage = 1;

    public async Task<Result<PagedNotificationsDto>> Handle(
        GetNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        // Validate and clamp page and pageSize
        var pageSize = Math.Clamp(query.PageSize, MinPageSize, MaxPageSize);
        var page = Math.Max(query.Page, MinPage);

        var user = loggedUserContext.GetUser();

        // Build base query with user filter (security)
        var notificationsQuery = context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == user.UserId);

        // Get total count before pagination
        var totalCount = await notificationsQuery.CountAsync(cancellationToken);

        // Apply sorting
        notificationsQuery = ApplySort(notificationsQuery, query.SortField, query.SortDirection);

        // Apply pagination and project to DTO
        var notifications = await notificationsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.HabitId,
                n.LocalDate,
                n.Type,
                n.Content,
                n.AiStatus,
                n.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedNotificationsDto(totalCount, notifications));
    }

    private static IQueryable<Data.Entities.Notification> ApplySort(
        IQueryable<Data.Entities.Notification> query,
        NotificationSortField field,
        SortDirection direction)
    {
        return (field, direction) switch
        {
            (NotificationSortField.CreatedAtUtc, SortDirection.Asc) => query.OrderBy(n => n.CreatedAtUtc),
            (NotificationSortField.CreatedAtUtc, SortDirection.Desc) => query.OrderByDescending(n => n.CreatedAtUtc),
            (NotificationSortField.LocalDate, SortDirection.Asc) => query.OrderBy(n => n.LocalDate),
            (NotificationSortField.LocalDate, SortDirection.Desc) => query.OrderByDescending(n => n.LocalDate),
            (NotificationSortField.Type, SortDirection.Asc) => query.OrderBy(n => n.Type),
            (NotificationSortField.Type, SortDirection.Desc) => query.OrderByDescending(n => n.Type),
            _ => query.OrderByDescending(n => n.CreatedAtUtc)
        };
    }
}
