using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Habits;

public record DeleteHabitCommand(
    int Id,
    string UserId
) : ICommand<Result>;

public class DeleteHabitCommandHandler(HabitFlowDbContext context)
    : ICommandHandler<DeleteHabitCommand, Result>
{
    public async Task<Result> Handle(DeleteHabitCommand command, CancellationToken cancellationToken)
    {
        // Find habit by ID and UserId (ownership check)
        var habit = await context.Habits
            .FirstOrDefaultAsync(h => h.Id == command.Id && h.UserId == command.UserId, cancellationToken);

        if (habit is null)
            return Result.Failure(Error.NotFound("Habit.NotFound", "Habit not found."));

        // Hard delete - cascades to Checkins and Notifications via FK constraints
        context.Habits.Remove(habit);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
