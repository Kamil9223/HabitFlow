using HabitFlow.Api.Contracts.Profile;

namespace HabitFlow.Api.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("/", () =>
            Results.StatusCode(501))
            .WithName("GetProfile")
            .Produces<ProfileResponse>(200)
            .Produces(401);

        group.MapPatch("/timezone", (UpdateTimeZoneRequest request) =>
            Results.StatusCode(501))
            .WithName("UpdateTimeZone")
            .Produces(204)
            .Produces(400)
            .Produces(422);
    }
}
