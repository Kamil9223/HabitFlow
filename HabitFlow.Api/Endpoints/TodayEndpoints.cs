using HabitFlow.Api.Contracts.Today;

namespace HabitFlow.Api.Endpoints;

public static class TodayEndpoints
{
    public static void MapTodayEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/today")
            .WithTags("Today")
            .RequireAuthorization();

        group.MapGet("/", (DateOnly? date) =>
            Results.StatusCode(501))
            .WithName("GetToday")
            .Produces<TodayResponse>(200)
            .Produces(401);
    }
}
