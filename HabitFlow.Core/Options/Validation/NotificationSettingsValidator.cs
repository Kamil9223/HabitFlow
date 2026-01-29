using Microsoft.Extensions.Options;

namespace HabitFlow.Core.Options.Validation;

public sealed class NotificationSettingsValidator : IValidateOptions<NotificationSettings>
{
    public ValidateOptionsResult Validate(string? name, NotificationSettings options)
    {
        if (options.Enabled)
        {
            if (options.BatchSize <= 0)
                return ValidateOptionsResult.Fail("NotificationSettings: BatchSize must be greater than 0 when enabled.");

            if (options.MaxExecutionMinutes <= 0)
                return ValidateOptionsResult.Fail("NotificationSettings: MaxExecutionMinutes must be greater than 0 when enabled.");

            if (string.IsNullOrWhiteSpace(options.CronSchedule) || !IsValidCronSchedule(options.CronSchedule))
                return ValidateOptionsResult.Fail("NotificationSettings: CronSchedule must use 'sec min hour * * ?' format.");

            if (options.AiNotificationsPerUserPerDay < 0)
                return ValidateOptionsResult.Fail("NotificationSettings: AiNotificationsPerUserPerDay must be >= 0.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsValidCronSchedule(string schedule)
    {
        var parts = schedule.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 6 && parts[3] == "*" && parts[4] == "*" && parts[5] == "?";
    }
}
