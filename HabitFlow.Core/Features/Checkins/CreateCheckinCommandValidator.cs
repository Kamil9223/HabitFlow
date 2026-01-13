using HabitFlow.Core.Common;

namespace HabitFlow.Core.Features.Checkins;

public static class CreateCheckinCommandValidator
{
    private const int MaxDaysBackfill = 7;

    public static List<Error> Validate(CreateCheckinCommand command)
    {
        var errors = new List<Error>();

        // Validate HabitId
        if (command.HabitId <= 0)
        {
            errors.Add(Error.Validation(
                nameof(command.HabitId),
                "HabitId must be greater than 0"));
        }

        // Validate ActualValue
        if (command.ActualValue < 0)
        {
            errors.Add(Error.Validation(
                nameof(command.ActualValue),
                "ActualValue must be non-negative"));
        }

        // Validate LocalDate - cannot be in the future
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (command.LocalDate > today)
        {
            errors.Add(Error.Validation(
                nameof(command.LocalDate),
                "LocalDate cannot be in the future"));
        }

        // Validate LocalDate - cannot be more than 7 days in the past
        var minDate = today.AddDays(-MaxDaysBackfill);
        if (command.LocalDate < minDate)
        {
            errors.Add(Error.Validation(
                nameof(command.LocalDate),
                $"LocalDate cannot be more than {MaxDaysBackfill} days in the past"));
        }

        return errors;
    }
}
