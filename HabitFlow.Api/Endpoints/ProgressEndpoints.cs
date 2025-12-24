using HabitFlow.Api.Contracts.Progress;

namespace HabitFlow.Api.Endpoints;

public static class ProgressEndpoints
{
    public static void MapProgressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/habits/{habitId:int}/progress")
            .WithTags("Progress")
            .RequireAuthorization();

        group.MapGet("/rolling", (int habitId, int windowDays, DateOnly? until) =>
            Results.StatusCode(501))
            .WithName("GetProgressRolling")
            .Produces<ProgressRollingResponse>(200)
            .Produces(400)
            .Produces(401)
            .Produces(404);
    }
}
