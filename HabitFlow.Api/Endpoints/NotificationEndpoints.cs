using HabitFlow.Api.Contracts.Common;
using HabitFlow.Api.Contracts.Notifications;

namespace HabitFlow.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/", (int? page, int? pageSize, string? sort) =>
            Results.StatusCode(501))
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
