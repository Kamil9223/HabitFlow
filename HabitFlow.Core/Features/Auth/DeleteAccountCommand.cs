using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Auth;

public record DeleteAccountCommand(
    string Confirmation
) : ICommand<Result>;

public class DeleteAccountCommandHandler(
    HabitFlowDbContext context,
    ILoggedUserContext loggedUserContext,
    UserManager<ApplicationUser> userManager)
    : ICommandHandler<DeleteAccountCommand, Result>
{
    public async Task<Result> Handle(DeleteAccountCommand command, CancellationToken cancellationToken)
    {
        // Validate confirmation
        if (command.Confirmation != "DELETE")
        {
            return Result.Failure(Error.Validation(
                "DeleteAccount.InvalidConfirmation",
                "Confirmation must be 'DELETE' to permanently delete account."));
        }

        var currentUser = loggedUserContext.GetUser();

        // Find user in database
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString());
        if (user is null)
            return Result.Failure(Error.NotFound("User.NotFound", "User not found."));

        // Delete all related data (habits, checkins, notifications)
        // They will be cascade deleted via FK constraints, but we can do it explicitly for clarity
        var habits = await context.Habits
            .Where(h => h.UserId == currentUser.UserId)
            .ToListAsync(cancellationToken);

        context.Habits.RemoveRange(habits);

        // Delete user account
        var deleteResult = await userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            return Result.Failure(Error.Failure(
                "DeleteAccount.Failed",
                $"Failed to delete account: {string.Join(", ", deleteResult.Errors.Select(e => e.Description))}"));
        }

        return Result.Success();
    }
}
