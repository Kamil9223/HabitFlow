namespace HabitFlow.Core.Options;

/// <summary>
/// Configuration for the notification generation background job.
/// </summary>
public sealed class NotificationJobSettings
{
    public const string SectionName = "NotificationJobSettings";

    public bool Enabled { get; set; } = true;
    public string CronSchedule { get; set; } = "0 30 0 * * ?";
    public int BatchSize { get; set; } = 100;
    public int MaxExecutionMinutes { get; set; } = 30;
}
