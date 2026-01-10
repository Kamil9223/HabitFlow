using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Common;
using HabitFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace HabitFlow.Core.Features.Auth;

public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword
) : ICommand<Result>;

public class ResetPasswordCommandHandler(
    UserManager<ApplicationUser> userManager)
    : ICommandHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        // Validate command
        var validationErrors = ResetPasswordValidator.Validate(command);
        if (validationErrors.Count > 0)
            return Result.Failure(validationErrors);

        // Find user by email
        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is null)
            return Result.Failure(
                Error.Validation("Auth.InvalidToken",
                    "Invalid or expired reset token."));

        // Attempt to reset password with token
        var result = await userManager.ResetPasswordAsync(user, command.Token, command.NewPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e =>
                Error.Validation($"Auth.{e.Code}", e.Description)).ToList();
            return Result.Failure(errors);
        }

        return Result.Success();
    }
}
