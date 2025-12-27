using HabitFlow.Api.Contracts.Common;
using HabitFlow.Api.Contracts.Habits;
using HabitFlow.Api.Helpers;
using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Common;
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

        group.MapGet("/", async (
            IQueryDispatcher dispatcher,
            int? page,
            int? pageSize,
            byte? type,
            byte? completionMode,
            bool? active,
            string? search,
            HabitSortField? sortField,
            SortDirection? sortDirection,
            CancellationToken cancellationToken) =>
        {
            // TODO: Get real UserId from authenticated user context
            var userId = "temp-user-id";

            var query = new GetHabitsQuery(
                userId,
                page ?? 1,
                pageSize ?? 20,
                type,
                completionMode,
                active,
                search,
                sortField ?? HabitSortField.CreatedAtUtc,
                sortDirection ?? SortDirection.Desc);

            var result = await dispatcher.Dispatch(query, cancellationToken);

            return result.ToHttpResult(pagedDto => Results.Ok(new PagedResponse<HabitResponse>(
                pagedDto.TotalCount,
                pagedDto.Items.Select(h => new HabitResponse(
                    h.Id,
                    h.Title,
                    h.Description,
                    h.Type,
                    h.CompletionMode,
                    h.DaysOfWeekMask,
                    h.TargetValue,
                    h.TargetUnit,
                    h.DeadlineDate,
                    new DateTimeOffset(h.CreatedAtUtc, TimeSpan.Zero)
                )).ToList()
            )));
        })
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

        group.MapGet("/{id:int}", async (
            int id,
            IQueryDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            // TODO: Get real UserId from authenticated user context
            var userId = "temp-user-id";

            var query = new GetHabitQuery(id, userId);

            var result = await dispatcher.Dispatch(query, cancellationToken);

            return result.ToHttpResult(habitDto => Results.Ok(new HabitResponse(
                habitDto.Id,
                habitDto.Title,
                habitDto.Description,
                habitDto.Type,
                habitDto.CompletionMode,
                habitDto.DaysOfWeekMask,
                habitDto.TargetValue,
                habitDto.TargetUnit,
                habitDto.DeadlineDate,
                new DateTimeOffset(habitDto.CreatedAtUtc, TimeSpan.Zero)
            )));
        })
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
