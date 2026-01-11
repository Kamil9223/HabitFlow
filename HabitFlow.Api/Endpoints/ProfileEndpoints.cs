using HabitFlow.Api.Contracts.Profile;
using HabitFlow.Api.Helpers;
using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Features.Profile;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Api.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("/", async (
            IQueryDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProfileQuery();
            var result = await dispatcher.Dispatch(query, cancellationToken);

            return result.ToHttpResult(value => Results.Ok(
                new ProfileResponse(
                    value.UserId,
                    value.Email,
                    value.EmailConfirmed,
                    value.TimeZoneId,
                    value.CreatedAtUtc,
                    value.HabitsCount)));
        })
        .WithName("GetProfile")
        .WithSummary("Get user profile information")
        .WithDescription("Returns full profile details including user information and habits count.")
        .Produces<ProfileResponse>(200)
        .Produces(401);

        group.MapPatch("/timezone", async (
            UpdateTimeZoneRequest request,
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateTimeZoneCommand(request.TimeZoneId);

            var result = await dispatcher.Dispatch(command, cancellationToken);

            return result.ToHttpResult(Results.NoContent);
        })
        .WithName("UpdateTimeZone")
        .WithSummary("Update user timezone")
        .WithDescription("Updates the user's timezone for habit tracking and notifications.")
        .Produces(204)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .Produces(422);

    }
}
