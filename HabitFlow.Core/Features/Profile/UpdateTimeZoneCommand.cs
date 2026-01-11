using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Profile;

public record UpdateTimeZoneCommand(
    string TimeZoneId
) : ICommand<Result>;

public class UpdateTimeZoneCommandHandler(
    HabitFlowDbContext context,
    ILoggedUserContext loggedUserContext)
    : ICommandHandler<UpdateTimeZoneCommand, Result>
{
    public async Task<Result> Handle(UpdateTimeZoneCommand command, CancellationToken cancellationToken)
    {
        // Validate command
        var validationErrors = UpdateTimeZoneValidator.Validate(command);
        if (validationErrors.Count > 0)
            return Result.Failure(validationErrors);

        var currentUser = loggedUserContext.GetUser();

        // Find user in database
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(Error.NotFound("User.NotFound", "User not found."));

        // Update timezone
        user.TimeZoneId = command.TimeZoneId;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
