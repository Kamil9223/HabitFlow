using HabitFlow.Api.Contracts.Checkins;

namespace HabitFlow.Api.Endpoints;

public static class CheckinEndpoints
{
    public static void MapCheckinEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1")
            .WithTags("Checkins")
            .RequireAuthorization();

        group.MapPost("/habits/{habitId:int}/checkins", (int habitId, CreateCheckinRequest request) =>
            Results.StatusCode(501))
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
