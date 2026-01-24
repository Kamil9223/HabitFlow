using HabitFlow.Core.Options;
using Microsoft.Extensions.Options;

namespace HabitFlow.Core.Options.Validation;

public sealed class NotificationJobSettingsValidator : IValidateOptions<NotificationJobSettings>
{
    public ValidateOptionsResult Validate(string? name, NotificationJobSettings options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        if (options.BatchSize <= 0)
            return ValidateOptionsResult.Fail("NotificationJobSettings: BatchSize must be greater than 0 when enabled.");

        if (options.MaxExecutionMinutes <= 0)
            return ValidateOptionsResult.Fail("NotificationJobSettings: MaxExecutionMinutes must be greater than 0 when enabled.");

        if (!CronScheduleValidator.IsValid(options.CronSchedule))
            return ValidateOptionsResult.Fail("NotificationJobSettings: CronSchedule must use 'sec min hour * * ?' format.");

        return ValidateOptionsResult.Success;
    }
}
