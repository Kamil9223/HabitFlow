using HabitFlow.Api.Contracts.Auth;
using HabitFlow.Api.Helpers;
using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Features.Auth;

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

            return result.ToHttpResult(Results.NoContent);
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

        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new ForgotPasswordCommand(request.Email);

            var result = await dispatcher.Dispatch(command, cancellationToken);

            return result.ToHttpResult(Results.NoContent);
        })
        .WithName("ForgotPassword")
        .Produces(204)
        .Produces(400);

        group.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new ResetPasswordCommand(
                request.Email,
                request.Token,
                request.NewPassword);

            var result = await dispatcher.Dispatch(command, cancellationToken);

            return result.ToHttpResult(Results.NoContent);
        })
        .WithName("ResetPassword")
        .Produces(204)
        .Produces(400);

        group.MapPost("/logout", async (
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.Dispatch(new LogoutCommand(), cancellationToken);

            return result.ToHttpResult(Results.NoContent);
        })
        .WithName("Logout")
        .WithSummary("End user session")
        .WithDescription("Invalidates current session/token. Client should discard stored tokens.")
        .Produces(204)
        .Produces(401)
        .RequireAuthorization();

        group.MapPost("/delete-account", async (
            DeleteAccountRequest request,
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteAccountCommand(request.Confirmation);

            var result = await dispatcher.Dispatch(command, cancellationToken);

            return result.ToHttpResult(Results.NoContent);
        })
        .WithName("DeleteAccount")
        .WithSummary("Permanently delete user account")
        .WithDescription(
            "Permanently deletes user account and all associated data (habits, check-ins, notifications). " +
            "Requires confirmation field with exact value 'DELETE'. This action cannot be undone.")
        .Produces(204)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization();
    }
}
