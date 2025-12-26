using HabitFlow.Core.Abstractions;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Habits;

public record CreateHabitCommand(
    string UserId,
    string Title,
    string? Description,
    byte Type,
    byte CompletionMode,
    byte DaysOfWeekMask,
    short TargetValue,
    string? TargetUnit,
    DateOnly? DeadlineDate
) : ICommand<Result<int>>;

public class CreateHabitCommandHandler(HabitFlowDbContext context)
    : ICommandHandler<CreateHabitCommand, Result<int>>
{
    private const int MaxHabitsPerUser = 20;

    public async Task<Result<int>> Handle(CreateHabitCommand command, CancellationToken cancellationToken)
    {
        // Validate command
        var validationErrors = CreateHabitValidator.Validate(command);
        if (validationErrors.Count > 0)
            return Result.Failure<int>(validationErrors);

        // Check habit limit for user
        var habitCount = await context.Habits
            .AsNoTracking()
            .CountAsync(h => h.UserId == command.UserId, cancellationToken);

        if (habitCount >= MaxHabitsPerUser)
            return Result.Failure<int>(
                Error.Conflict("Habit.LimitExceeded",
                    $"Cannot create more than {MaxHabitsPerUser} habits per user."));

        // Create habit entity
        var habit = new Habit
        {
            UserId = command.UserId,
            Title = command.Title,
            Description = command.Description,
            Type = command.Type,
            CompletionMode = command.CompletionMode,
            DaysOfWeekMask = command.DaysOfWeekMask,
            TargetValue = command.TargetValue,
            TargetUnit = command.TargetUnit,
            DeadlineDate = command.DeadlineDate,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.Add(habit);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(habit.Id);
    }
}