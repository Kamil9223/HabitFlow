namespace HabitFlow.Core.Options;

/// <summary>
/// Feature flags for notifications and AI generation.
/// </summary>
public sealed class NotificationFeaturesOptions
{
    public const string SectionName = "Features";

    public bool NotificationsEnabled { get; set; } = true;
    public AiNotificationsOptions AiNotifications { get; set; } = new();

    public sealed class AiNotificationsOptions
    {
        public bool Enabled { get; set; } = true;
        public bool FallbackOnly { get; set; } = false;
    }
}
