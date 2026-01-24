using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace HabitFlow.Core.Features.Auth;

public record ResendConfirmationCommand : ICommand<Result>;

public class ResendConfirmationCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    ILoggedUserContext loggedUserContext)
    : ICommandHandler<ResendConfirmationCommand, Result>
{
    public async Task<Result> Handle(ResendConfirmationCommand command, CancellationToken cancellationToken)
    {
        // Get current user from authentication context
        var currentUser = loggedUserContext.GetUser();
        if (currentUser.UserId == Guid.Empty)
            return Result.Failure(
                Error.Unauthorized("Auth.Unauthorized",
                    "User is not authenticated."));

        // Find user by id
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString());
        if (user is null)
            return Result.Failure(
                Error.NotFound("User.NotFound",
                    "User not found."));

        // Check if email is already confirmed
        if (user.EmailConfirmed)
            return Result.Failure(
                Error.Conflict("User.EmailAlreadyConfirmed",
                    "Email address has already been confirmed."));

        // Generate new email confirmation token
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        // Build confirmation link
        // Format: /auth/confirm-email?userId={userId}&token={token}
        var encodedToken = Uri.EscapeDataString(token);
        var confirmationLink = $"/auth/confirm-email?userId={user.Id}&token={encodedToken}";

        // Send confirmation email
        await emailSender.SendEmailConfirmationAsync(user.Email!, confirmationLink, cancellationToken);

        return Result.Success();
    }
}
