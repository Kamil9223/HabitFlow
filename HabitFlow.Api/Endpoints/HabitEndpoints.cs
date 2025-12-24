using HabitFlow.Api.Contracts.Common;
using HabitFlow.Api.Contracts.Habits;

namespace HabitFlow.Api.Endpoints;

public static class HabitEndpoints
{
    public static void MapHabitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/habits")
            .WithTags("Habits")
            .RequireAuthorization();

        group.MapGet("/", (int? page, int? pageSize, int? type, int? completionMode, bool? active, string? search, string? sort) =>
            Results.StatusCode(501))
            .WithName("GetHabits")
            .Produces<PagedResponse<HabitResponse>>(200)
            .Produces(401);

        group.MapPost("/", (CreateHabitRequest request) =>
            Results.StatusCode(501))
            .WithName("CreateHabit")
            .Produces<HabitResponse>(201)
            .Produces(400)
            .Produces(401)
            .Produces(409);

        group.MapGet("/{id:int}", (int id) =>
            Results.StatusCode(501))
            .WithName("GetHabit")
            .Produces<HabitResponse>(200)
            .Produces(401)
            .Produces(404);

        group.MapPatch("/{id:int}", (int id, UpdateHabitRequest request) =>
            Results.StatusCode(501))
            .WithName("UpdateHabit")
            .Produces<HabitResponse>(200)
            .Produces(400)
            .Produces(401)
            .Produces(404);

        group.MapDelete("/{id:int}", (int id) =>
            Results.StatusCode(501))
            .WithName("DeleteHabit")
            .Produces(204)
            .Produces(401)
            .Produces(404);

        group.MapGet("/{id:int}/calendar", (int id, DateOnly from, DateOnly to) =>
            Results.StatusCode(501))
            .WithName("GetHabitCalendar")
            .Produces<HabitCalendarResponse>(200)
            .Produces(400)
            .Produces(401)
            .Produces(404);
    }
}
