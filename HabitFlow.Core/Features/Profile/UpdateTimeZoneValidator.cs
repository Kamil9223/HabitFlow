using HabitFlow.Core.Common;

namespace HabitFlow.Core.Features.Profile;

public static class UpdateTimeZoneValidator
{
    private const int MaxTimeZoneIdLength = 100;

    public static List<Error> Validate(UpdateTimeZoneCommand command)
    {
        var errors = new List<Error>();

        // TimeZoneId validation
        if (string.IsNullOrWhiteSpace(command.TimeZoneId))
        {
            errors.Add(Error.Validation(
                "TimeZone.Required",
                "TimeZoneId is required."));
            return errors;
        }

        if (command.TimeZoneId.Length > MaxTimeZoneIdLength)
        {
            errors.Add(Error.Validation(
                "TimeZone.TooLong",
                $"TimeZoneId must not exceed {MaxTimeZoneIdLength} characters."));
        }

        // Validate that the timezone is a valid IANA timezone
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(command.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            errors.Add(Error.Validation(
                "TimeZone.Invalid",
                $"TimeZoneId '{command.TimeZoneId}' is not a valid IANA timezone identifier."));
        }
        catch (InvalidTimeZoneException)
        {
            errors.Add(Error.Validation(
                "TimeZone.Invalid",
                $"TimeZoneId '{command.TimeZoneId}' is not a valid timezone identifier."));
        }

        return errors;
    }
}
