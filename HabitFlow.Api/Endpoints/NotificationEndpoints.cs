using HabitFlow.Api.Contracts.Common;
using HabitFlow.Api.Contracts.Notifications;
using HabitFlow.Api.Helpers;
using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Common;
using HabitFlow.Core.Features.Notifications;

namespace HabitFlow.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/", async (
            IQueryDispatcher dispatcher,
            int? page,
            int? pageSize,
            NotificationSortField? sortField,
            SortDirection? sortDirection,
            CancellationToken cancellationToken) =>
        {
            var query = new GetNotificationsQuery(
                page ?? 1,
                pageSize ?? 20,
                sortField ?? NotificationSortField.CreatedAtUtc,
                sortDirection ?? SortDirection.Desc);

            var result = await dispatcher.Dispatch(query, cancellationToken);

            return result.ToHttpResult(pagedDto => Results.Ok(new PagedResponse<NotificationResponse>(
                pagedDto.TotalCount,
                pagedDto.Items.Select(n => new NotificationResponse(
                    n.Id,
                    n.HabitId,
                    n.LocalDate,
                    (int)n.Type,
                    n.Content,
                    (int?)n.AiStatus,
                    new DateTimeOffset(n.CreatedAtUtc, TimeSpan.Zero)
                )).ToList()
            )));
        })
        .WithName("GetNotifications")
        .Produces<PagedResponse<NotificationResponse>>(200)
        .Produces(401);

        group.MapGet("/{id:long}", (long id) =>
            Results.StatusCode(501))
            .WithName("GetNotification")
            .Produces<NotificationResponse>(200)
            .Produces(401)
            .Produces(404);
    }
}
