using HabitFlow.Api.Contracts.Auth;

namespace HabitFlow.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", (RegisterRequest request) =>
            Results.StatusCode(501))
            .WithName("Register")
            .Produces<RegisterResponse>(201)
            .Produces(400)
            .Produces(409)
            .Produces(422);

        group.MapPost("/confirm-email", (ConfirmEmailRequest request) =>
            Results.StatusCode(501))
            .WithName("ConfirmEmail")
            .Produces(204)
            .Produces(400)
            .Produces(404)
            .Produces(409);

        group.MapPost("/login", (LoginRequest request) =>
            Results.StatusCode(501))
            .WithName("Login")
            .Produces<LoginResponse>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403);

        group.MapPost("/refresh", (RefreshRequest request) =>
            Results.StatusCode(501))
            .WithName("RefreshToken")
            .Produces<RefreshResponse>(200)
            .Produces(400)
            .Produces(401)
            .Produces(409);

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
    }
}
