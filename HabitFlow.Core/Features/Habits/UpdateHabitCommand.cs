using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Habits;

public record UpdateHabitCommand(
    int Id,
    string UserId,
    string? Title,
    string? Description,
    byte? Type,
    byte? CompletionMode,
    byte? DaysOfWeekMask,
    short? TargetValue,
    string? TargetUnit,
    DateOnly? DeadlineDate,
    bool ClearDeadlineDate
) : ICommand<Result<int>>;

public class UpdateHabitCommandHandler(HabitFlowDbContext context)
    : ICommandHandler<UpdateHabitCommand, Result<int>>
{
    public async Task<Result<int>> Handle(UpdateHabitCommand command, CancellationToken cancellationToken)
    {
        // Validate command
        var validationErrors = UpdateHabitValidator.Validate(command);
        if (validationErrors.Count > 0)
            return Result.Failure<int>(validationErrors);

        // Find habit by ID and UserId (ownership check)
        var habit = await context.Habits
            .FirstOrDefaultAsync(h => h.Id == command.Id && h.UserId == command.UserId, cancellationToken);

        if (habit is null)
            return Result.Failure<int>(Error.NotFound("Habit.NotFound", "Habit not found."));

        // Apply updates only for provided fields
        if (command.Title is not null)
            habit.Title = command.Title;

        if (command.Description is not null)
            habit.Description = command.Description;

        if (command.Type.HasValue)
            habit.Type = command.Type.Value;

        if (command.CompletionMode.HasValue)
            habit.CompletionMode = command.CompletionMode.Value;

        if (command.DaysOfWeekMask.HasValue)
            habit.DaysOfWeekMask = command.DaysOfWeekMask.Value;

        if (command.TargetValue.HasValue)
            habit.TargetValue = command.TargetValue.Value;

        if (command.TargetUnit is not null)
            habit.TargetUnit = command.TargetUnit;

        if (command.DeadlineDate.HasValue)
            habit.DeadlineDate = command.DeadlineDate.Value;
        else if (command.ClearDeadlineDate)
            habit.DeadlineDate = null;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(habit.Id);
    }
}
