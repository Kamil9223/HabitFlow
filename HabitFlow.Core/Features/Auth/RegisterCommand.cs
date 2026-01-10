using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace HabitFlow.Core.Features.Auth;

public record RegisterCommand(
    string Email,
    string Password,
    string? DisplayName
) : ICommand<Result<RegisterResult>>;

public record RegisterResult(
    Guid UserId,
    string Email,
    bool EmailConfirmed
);

public class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender)
    : ICommandHandler<RegisterCommand, Result<RegisterResult>>
{
    public async Task<Result<RegisterResult>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        // Validate command
        var validationErrors = RegisterValidator.Validate(command);
        if (validationErrors.Count > 0)
            return Result.Failure<RegisterResult>(validationErrors);

        // Check if email already exists
        var existingUser = await userManager.FindByEmailAsync(command.Email);
        if (existingUser is not null)
            return Result.Failure<RegisterResult>(
                Error.Conflict("User.EmailAlreadyExists",
                    "A user with this email address already exists."));

        // Create user entity
        var user = new ApplicationUser
        {
            Email = command.Email,
            UserName = command.Email, // Using email as username
            TimeZoneId = "UTC", // Default timezone
            CreatedAtUtc = DateTime.UtcNow
        };

        // Create user with password
        var result = await userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e =>
                Error.Validation($"User.{e.Code}", e.Description)).ToList();
            return Result.Failure<RegisterResult>(errors);
        }

        // Update DisplayName if provided (UserManager.CreateAsync doesn't set custom properties)
        if (!string.IsNullOrWhiteSpace(command.DisplayName))
        {
            user.UserName = command.DisplayName;
            await userManager.UpdateAsync(user);
        }

        // Generate email confirmation token
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        // Build confirmation link (will be used by email sender)
        // Format: /auth/confirm-email?userId={userId}&token={token}
        var encodedToken = Uri.EscapeDataString(token);
        var confirmationLink = $"/auth/confirm-email?userId={user.Id}&token={encodedToken}";

        // Send confirmation email
        await emailSender.SendEmailConfirmationAsync(user.Email!, confirmationLink, cancellationToken);

        return Result.Success(new RegisterResult(
            user.Id,
            user.Email!,
            user.EmailConfirmed
        ));
    }
}
