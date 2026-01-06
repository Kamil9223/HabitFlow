using HabitFlow.Api.Contracts.Auth;
using HabitFlow.Api.Helpers;
using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Features.Auth;
using HabitFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterCommand(
                request.Email,
                request.Password,
                request.DisplayName);

            var result = await dispatcher.Dispatch(command, cancellationToken);

            return result.ToHttpResult(value => Results.Created(
                $"/api/v1/auth/me",
                new RegisterResponse(value.UserId, value.Email, value.EmailConfirmed)));
        })
            .WithName("Register")
            .Produces<RegisterResponse>(201)
            .Produces(400)
            .Produces(409)
            .Produces(422);

        group.MapPost("/confirm-email", async (
            ConfirmEmailRequest request,
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new ConfirmEmailCommand(
                request.UserId,
                request.Token);

            var result = await dispatcher.Dispatch(command, cancellationToken);

            return result.ToHttpResult(_ => Results.NoContent());
        })
            .WithName("ConfirmEmail")
            .Produces(204)
            .Produces(400)
            .Produces(404)
            .Produces(409);

        group.MapPost("/login", async (
            LoginRequest request,
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginCommand(
                request.Email,
                request.Password);

            var result = await dispatcher.Dispatch(command, cancellationToken);

            return result.ToHttpResult(value => Results.Ok(
                new LoginResponse(value.UserId, value.Email, value.EmailConfirmed)));
        })
            .WithName("Login")
            .Produces<LoginResponse>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403);

        group.MapPost("/forgot-password", (ForgotPasswordRequest request) =>
            Results.StatusCode(501))
            .WithName("ForgotPassword")
            .Produces(204)
            .Produces(400);

        group.MapPost("/reset-password", (ResetPasswordRequest request) =>
            Results.StatusCode(501))
            .WithName("ResetPassword")
            .Produces(204)
            .Produces(400);

        group.MapGet("/me", () =>
            Results.StatusCode(501))
            .WithName("GetMe")
            .Produces<MeResponse>(200)
            .Produces(401)
            .RequireAuthorization();

        group.MapPost("/logout", () =>
        {
            // TODO: Get real UserId from authenticated user context
            // TODO: Implement logout logic (invalidate token/session)
            // For JWT: typically client-side token deletion
            // For session-based: invalidate server session

            return Results.NoContent();
        })
            .WithName("Logout")
            .WithSummary("End user session")
            .WithDescription("Invalidates current session/token. Client should discard stored tokens.")
            .Produces(204)
            .Produces(401)
            .RequireAuthorization();

        group.MapPost("/delete-account", ([FromBody] DeleteAccountRequest request) =>
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
            .Produces(401)
            .RequireAuthorization();
    }
}
