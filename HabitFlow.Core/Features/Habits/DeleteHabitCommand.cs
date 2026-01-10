using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Habits;

public record DeleteHabitCommand(
    int Id
) : ICommand<Result>;

public class DeleteHabitCommandHandler(
    HabitFlowDbContext context,
    ILoggedUserContext loggedUserContext)
    : ICommandHandler<DeleteHabitCommand, Result>
{
    public async Task<Result> Handle(DeleteHabitCommand command, CancellationToken cancellationToken)
    {
        var user = loggedUserContext.GetUser();
        
        // Find habit by ID and UserId (ownership check)
        var habit = await context.Habits
            .FirstOrDefaultAsync(h => h.Id == command.Id && h.UserId == user.UserId, cancellationToken);

        if (habit is null)
            return Result.Failure(Error.NotFound("Habit.NotFound", "Habit not found."));

        // Hard delete - cascades to Checkins and Notifications via FK constraints
        context.Habits.Remove(habit);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
