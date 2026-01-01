using HabitFlow.Api.Contracts.Profile;
using Microsoft.AspNetCore.Mvc;

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

        group.MapDelete("/", ([FromBody] DeleteAccountRequest request) =>
        {
            if (request.Confirmation != "DELETE")
            {
                return Results.Problem(
                    title: "Invalid confirmation",
                    detail: "Please provide 'DELETE' in the confirmation field to permanently delete your account.",
                    statusCode: 400
                );
            }

            // TODO: Implement DeleteAccountCommand
            // var command = new DeleteAccountCommand(userId);
            // var result = await dispatcher.Dispatch(command, cancellationToken);
            // return result.ToHttpResult(Results.NoContent);

            return Results.NoContent();
        })
            .WithName("DeleteAccount")
            .WithSummary("Permanently delete user account")
            .WithDescription(
                "Permanently deletes user account and all associated data (habits, check-ins, notifications). " +
                "Requires confirmation field with exact value 'DELETE'. This action cannot be undone.")
            .Produces(204)
            .Produces(400)
            .Produces(401);
    }
}
