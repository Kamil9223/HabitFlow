using HabitFlow.Core.Common;

namespace HabitFlow.Core.Features.Checkins;

public static class GetCheckinsQueryValidator
{
    private const int MaxDateRangeDays = 365;

    public static List<Error> Validate(GetCheckinsQuery query)
    {
        var errors = new List<Error>();

        // Validate HabitId
        if (query.HabitId <= 0)
        {
            errors.Add(Error.Validation(
                nameof(query.HabitId),
                "HabitId must be greater than 0"));
        }

        // Validate date range: from <= to
        if (query.From > query.To)
        {
            errors.Add(Error.Validation(
                nameof(query.From),
                "Date 'from' cannot be after 'to'"));
        }

        // Validate date range: cannot exceed 365 days
        var daysDifference = query.To.DayNumber - query.From.DayNumber;
        if (daysDifference > MaxDateRangeDays)
        {
            errors.Add(Error.Validation(
                "DateRange",
                $"Date range cannot exceed {MaxDateRangeDays} days"));
        }

        return errors;
    }
}
