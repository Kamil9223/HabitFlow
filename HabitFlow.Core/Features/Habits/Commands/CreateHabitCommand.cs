using HabitFlow.Core.Abstractions;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Habits.Commands;

public record CreateHabitCommand(
    string UserId,
    string Title,
    string? Description,
    int Type,
    int CompletionMode,
    int DaysOfWeekMask,
    int TargetValue,
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
        var validationResult = CreateHabitValidator.Validate(command);
        if (validationResult.IsFailure)
            return Result.Failure<int>(validationResult.Error);

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
            Type = (byte)command.Type,
            CompletionMode = (byte)command.CompletionMode,
            DaysOfWeekMask = (byte)command.DaysOfWeekMask,
            TargetValue = (short)command.TargetValue,
            TargetUnit = command.TargetUnit,
            DeadlineDate = command.DeadlineDate,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.Add(habit);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(habit.Id);
    }
}

public static class CreateHabitValidator
{
    public static Result<CreateHabitCommand> Validate(CreateHabitCommand command)
    {
        // Title validation
        if (string.IsNullOrWhiteSpace(command.Title))
            return Result.Failure<CreateHabitCommand>(
                Error.Validation("Habit.TitleRequired", "Title is required."));

        if (command.Title.Length > 80)
            return Result.Failure<CreateHabitCommand>(
                Error.Validation("Habit.TitleTooLong", "Title must not exceed 80 characters."));

        // Description validation
        if (command.Description?.Length > 280)
            return Result.Failure<CreateHabitCommand>(
                Error.Validation("Habit.DescriptionTooLong", "Description must not exceed 280 characters."));

        // Type validation (1 = Start, 2 = Stop)
        if (command.Type < 1 || command.Type > 2)
            return Result.Failure<CreateHabitCommand>(
                Error.Validation("Habit.InvalidType", "Type must be 1 (Start) or 2 (Stop)."));

        // CompletionMode validation (1 = Binary, 2 = Quantitative, 3 = Checklist)
        if (command.CompletionMode < 1 || command.CompletionMode > 3)
            return Result.Failure<CreateHabitCommand>(
                Error.Validation("Habit.InvalidCompletionMode",
                    "CompletionMode must be 1 (Binary), 2 (Quantitative), or 3 (Checklist)."));

        // DaysOfWeekMask validation (1-127, bitmask for 7 days)
        if (command.DaysOfWeekMask < 1 || command.DaysOfWeekMask > 127)
            return Result.Failure<CreateHabitCommand>(
                Error.Validation("Habit.InvalidDaysOfWeekMask", "DaysOfWeekMask must be between 1 and 127."));

        // TargetValue validation (1-100 per API spec)
        if (command.TargetValue < 1 || command.TargetValue > 100)
            return Result.Failure<CreateHabitCommand>(
                Error.Validation("Habit.InvalidTargetValue", "TargetValue must be between 1 and 100."));

        // TargetUnit validation
        if (command.TargetUnit?.Length > 32)
            return Result.Failure<CreateHabitCommand>(
                Error.Validation("Habit.TargetUnitTooLong", "TargetUnit must not exceed 32 characters."));

        // DeadlineDate validation (must be in the future if provided)
        if (command.DeadlineDate.HasValue && command.DeadlineDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            return Result.Failure<CreateHabitCommand>(
                Error.Validation("Habit.InvalidDeadlineDate", "DeadlineDate must be in the future."));

        return Result.Success(command);
    }
}
