namespace HabitFlow.Core.Options;

/// <summary>
/// Simplified configuration for notification generation system.
/// Replaces NotificationJobSettings, NotificationFeaturesOptions, and parts of LlmSettings.
/// </summary>
public sealed class NotificationSettings
{
    public const string SectionName = "NotificationSettings";

    /// <summary>
    /// Master switch for entire notification system.
    /// If false, background job does not run.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cron schedule for daily job execution (default: 00:30 UTC).
    /// Format: "minute hour day month dayOfWeek"
    /// Example: "0 30 0 * * ?" = 00:30 every day
    /// </summary>
    public string CronSchedule { get; set; } = "0 30 0 * * ?";

    /// <summary>
    /// Number of users to process in each batch.
    /// Default: 100
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum job execution time in minutes before timeout.
    /// Default: 30 minutes
    /// </summary>
    public int MaxExecutionMinutes { get; set; } = 30;

    /// <summary>
    /// Maximum number of AI-generated notifications per user per day.
    /// After reaching this limit, user receives template-based notifications.
    /// This provides fair allocation and predictable cost control.
    /// Default: 3 per user per day
    /// </summary>
    public int AiNotificationsPerUserPerDay { get; set; } = 3;
}
