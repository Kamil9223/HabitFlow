using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace HabitFlow.Core.Features.Auth;

public record ForgotPasswordCommand(
    string Email
) : ICommand<Result>;

public class ForgotPasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender)
    : ICommandHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        // Validate command
        var validationErrors = ForgotPasswordValidator.Validate(command);
        if (validationErrors.Count > 0)
            return Result.Failure(validationErrors);

        // Find user by email (security: don't reveal if email exists)
        var user = await userManager.FindByEmailAsync(command.Email);

        // Always return success to prevent user enumeration
        // Only send email if user exists
        if (user is not null)
        {
            // Generate password reset token
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            // Build reset link
            // Format: /auth/reset-password?email={email}&token={token}
            var encodedToken = Uri.EscapeDataString(token);
            var encodedEmail = Uri.EscapeDataString(user.Email!);
            var resetLink = $"/auth/reset-password?email={encodedEmail}&token={encodedToken}";

            // Send password reset email
            await emailSender.SendPasswordResetAsync(user.Email!, resetLink, cancellationToken);
        }

        // Always return success (security requirement from auth-spec.md)
        return Result.Success();
    }
}
