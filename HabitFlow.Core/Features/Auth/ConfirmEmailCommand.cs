using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Common;
using HabitFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace HabitFlow.Core.Features.Auth;

public record ConfirmEmailCommand(
    string UserId,
    string Token
) : ICommand<Result<ConfirmEmailResult>>;

public record ConfirmEmailResult(
    Guid UserId,
    string Email,
    bool EmailConfirmed
);

public class ConfirmEmailCommandHandler(
    UserManager<ApplicationUser> userManager)
    : ICommandHandler<ConfirmEmailCommand, Result<ConfirmEmailResult>>
{
    public async Task<Result<ConfirmEmailResult>> Handle(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        // Validate command
        var validationErrors = ConfirmEmailValidator.Validate(command);
        if (validationErrors.Count > 0)
            return Result.Failure<ConfirmEmailResult>(validationErrors);

        // Find user by id
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return Result.Failure<ConfirmEmailResult>(
                Error.NotFound("User.NotFound",
                    "User not found."));

        // Check if email is already confirmed
        if (user.EmailConfirmed)
            return Result.Failure<ConfirmEmailResult>(
                Error.Conflict("User.EmailAlreadyConfirmed",
                    "Email address has already been confirmed."));

        // Confirm email with token
        var result = await userManager.ConfirmEmailAsync(user, command.Token);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e =>
                Error.Validation($"ConfirmEmail.{e.Code}", e.Description)).ToList();
            return Result.Failure<ConfirmEmailResult>(errors);
        }

        return Result.Success(new ConfirmEmailResult(
            user.Id,
            user.Email!,
            user.EmailConfirmed
        ));
    }
}
