using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Common;
using HabitFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace HabitFlow.Core.Features.Auth;

public record LoginCommand(
    string Email,
    string Password
) : ICommand<Result<LoginResult>>;

public record LoginResult(
    string UserId,
    string Email,
    bool EmailConfirmed
);

public class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager)
    : ICommandHandler<LoginCommand, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        // Validate command
        var validationErrors = LoginValidator.Validate(command);
        if (validationErrors.Count > 0)
            return Result.Failure<LoginResult>(validationErrors);

        // Find user by email
        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is null)
            return Result.Failure<LoginResult>(
                Error.Unauthorized("Auth.InvalidCredentials",
                    "Invalid email or password."));

        // Check if email is confirmed
        if (!user.EmailConfirmed)
            return Result.Failure<LoginResult>(
                Error.Forbidden("Auth.EmailNotConfirmed",
                    "Email address has not been confirmed. Please check your email for the confirmation link."));

        // Attempt to sign in (sets cookie)
        var result = await signInManager.PasswordSignInAsync(
            user,
            command.Password,
            isPersistent: true, // Keep user logged in across browser sessions
            lockoutOnFailure: false); // No lockout in MVP

        if (!result.Succeeded)
            return Result.Failure<LoginResult>(
                Error.Unauthorized("Auth.InvalidCredentials",
                    "Invalid email or password."));

        return Result.Success(new LoginResult(
            user.Id,
            user.Email!,
            user.EmailConfirmed
        ));
    }
}
