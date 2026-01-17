using HabitFlow.Api.Contracts.Progress;
using HabitFlow.Api.Helpers;
using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Features.Progress;

namespace HabitFlow.Api.Endpoints;

public static class ProgressEndpoints
{
    public static void MapProgressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/habits/{habitId:int}/progress")
            .WithTags("Progress")
            .RequireAuthorization();

        group.MapGet("/rolling", async (
            int habitId,
            int windowDays,
            DateOnly? until,
            IQueryDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProgressRollingQuery(habitId, windowDays, until);
            var result = await dispatcher.Dispatch(query, cancellationToken);

            return result.ToHttpResult(data => Results.Ok(
                new ProgressRollingResponse(
                    data.HabitId,
                    data.WindowDays,
                    data.Until,
                    data.Points.Select(p => new ProgressRollingPoint(
                        p.Date,
                        p.PlannedDays,
                        p.SumDailyScore,
                        p.SuccessRate
                    )).ToList()
                )));
        })
            .WithName("GetProgressRolling")
            .Produces<ProgressRollingResponse>(200)
            .Produces(400)
            .Produces(401)
            .Produces(404);
    }
}
