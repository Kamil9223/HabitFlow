namespace HabitFlow.Core.Options.Validation;

public static class CronScheduleValidator
{
    public static bool IsValid(string? cronSchedule)
    {
        if (string.IsNullOrWhiteSpace(cronSchedule))
            return false;

        var parts = cronSchedule.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            return false;

        if (!int.TryParse(parts[1], out var minute) || !int.TryParse(parts[2], out var hour))
            return false;

        return hour is >= 0 and <= 23 && minute is >= 0 and <= 59;
    }
}
