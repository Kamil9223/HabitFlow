using HabitFlow.Core.Common;

namespace HabitFlow.Core.Features.Progress;

public static class GetProgressRollingValidator
{
    public static List<Error> Validate(GetProgressRollingQuery query)
    {
        var errors = new List<Error>();

        // Validate HabitId
        if (query.HabitId <= 0)
        {
            errors.Add(Error.Validation(
                nameof(query.HabitId),
                "HabitId must be greater than 0"));
        }

        // Validate WindowDays: must be 7 or 30
        if (query.WindowDays != 7 && query.WindowDays != 30)
        {
            errors.Add(Error.Validation(
                nameof(query.WindowDays),
                "WindowDays must be 7 or 30"));
        }

        // Validate Until: cannot be in the future
        // Note: We cannot check against user's "today" here because we don't have access to timezone yet
        // This will be validated in the handler if needed, or we accept that validation happens there

        return errors;
    }
}
