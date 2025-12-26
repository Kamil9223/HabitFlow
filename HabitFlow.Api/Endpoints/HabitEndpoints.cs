using HabitFlow.Api.Contracts.Common;
using HabitFlow.Api.Contracts.Habits;
using HabitFlow.Api.Helpers;
using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Features.Habits;
using Microsoft.AspNetCore.Mvc;

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

        group.MapPost("/", async (
            CreateHabitRequest request,
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            // TODO: Get real UserId from authenticated user context
            var userId = "temp-user-id";

            var command = new CreateHabitCommand(
                userId,
                request.Title,
                request.Description,
                request.Type,
                request.CompletionMode,
                request.DaysOfWeekMask,
                request.TargetValue,
                request.TargetUnit,
                request.DeadlineDate);

            var result = await dispatcher.Dispatch(command, cancellationToken);
            return result.ToHttpResult(id => Results.Created($"/api/v1/habits/{id}", new { id }));
        })
            .WithName("CreateHabit")
            .Produces<object>(201)
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
