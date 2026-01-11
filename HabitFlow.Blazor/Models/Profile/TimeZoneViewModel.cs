namespace HabitFlow.Blazor.Models.Profile;

/// <summary>
/// Model pomocniczy reprezentujący strefę czasową dla listy rozwijanej.
/// </summary>
public class TimeZoneViewModel
{
    /// <summary>
    /// Identyfikator IANA strefy czasowej (np. "America/New_York").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Przyjazna nazwa strefy czasowej do wyświetlenia
    /// (np. "(UTC-05:00) Eastern Time (US & Canada)").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}
