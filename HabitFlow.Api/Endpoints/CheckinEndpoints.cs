using HabitFlow.Api.Contracts.Checkins;
using HabitFlow.Api.Helpers;
using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Features.Checkins;

namespace HabitFlow.Api.Endpoints;

public static class CheckinEndpoints
{
    public static void MapCheckinEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1")
            .WithTags("Checkins")
            .RequireAuthorization();

        group.MapPost("/habits/{habitId:int}/checkins", async (
            int habitId,
            CreateCheckinRequest request,
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateCheckinCommand(
                habitId,
                request.LocalDate,
                request.ActualValue);

            var result = await dispatcher.Dispatch(command, cancellationToken);

            return result.ToHttpResult(checkin => Results.Created(
                $"/api/v1/habits/{habitId}/checkins/{checkin.Id}",
                new CheckinResponse(
                    checkin.Id,
                    checkin.HabitId,
                    checkin.LocalDate,
                    checkin.ActualValue,
                    checkin.TargetValueSnapshot,
                    checkin.CompletionModeSnapshot,
                    checkin.HabitTypeSnapshot,
                    checkin.IsPlanned,
                    checkin.CreatedAtUtc)));
        })
        .WithName("CreateCheckin")
        .Produces<CheckinResponse>(201)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(409)
        .Produces(422);

        group.MapGet("/habits/{habitId:int}/checkins", (int habitId, DateOnly from, DateOnly to) =>
            Results.StatusCode(501))
            .WithName("GetCheckins")
            .Produces<CheckinListResponse>(200)
            .Produces(400)
            .Produces(401)
            .Produces(404);

        group.MapGet("/checkins", (DateOnly date) =>
            Results.StatusCode(501))
            .WithName("GetCheckinsByDate")
            .Produces<CheckinsByDateResponse>(200)
            .Produces(400)
            .Produces(401);
    }
}
