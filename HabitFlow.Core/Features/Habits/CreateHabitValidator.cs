using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Common;

namespace HabitFlow.Core.Features.Habits;

public static class CreateHabitValidator
{
    public static IReadOnlyList<Error> Validate(CreateHabitCommand command)
    {
        var errors = new List<Error>();
        
        // Title validation
        if (string.IsNullOrWhiteSpace(command.Title))
            errors.Add(Error.Validation("Habit.TitleRequired", "Title is required."));

        if (command.Title.Length > 80)
            errors.Add(Error.Validation("Habit.TitleTooLong", "Title must not exceed 80 characters."));

        // Description validation
        if (command.Description?.Length > 280)
            errors.Add(Error.Validation("Habit.DescriptionTooLong", "Description must not exceed 280 characters."));

        // Type validation (1 = Start, 2 = Stop)
        if (command.Type is < 1 or > 2)
            errors.Add(Error.Validation("Habit.InvalidType", "Type must be 1 (Start) or 2 (Stop)."));

        // CompletionMode validation (1 = Binary, 2 = Quantitative, 3 = Checklist)
        if (command.CompletionMode is < 1 or > 3)
            errors.Add(Error.Validation("Habit.InvalidCompletionMode",
                "CompletionMode must be 1 (Binary), 2 (Quantitative), or 3 (Checklist)."));

        // DaysOfWeekMask validation (1-127, bitmask for 7 days)
        if (command.DaysOfWeekMask is < 1 or > 127)
            errors.Add(Error.Validation("Habit.InvalidDaysOfWeekMask", "DaysOfWeekMask must be between 1 and 127."));

        // TargetValue validation (1-100 per API spec)
        if (command.TargetValue is < 1 or > 100)
            errors.Add(Error.Validation("Habit.InvalidTargetValue", "TargetValue must be between 1 and 100."));

        // TargetUnit validation
        if (command.TargetUnit?.Length > 32)
            errors.Add(Error.Validation("Habit.TargetUnitTooLong", "TargetUnit must not exceed 32 characters."));

        // DeadlineDate validation (must be in the future if provided)
        if (command.DeadlineDate.HasValue && command.DeadlineDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            errors.Add(Error.Validation("Habit.InvalidDeadlineDate", "DeadlineDate must be in the future."));

        return errors;
    }
}