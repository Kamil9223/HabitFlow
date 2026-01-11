namespace HabitFlow.Blazor.Models.Profile;

/// <summary>
/// Model widoku przechowujący dane użytkownika pobrane z API profilu.
/// Mapowany z ProfileResponse.
/// </summary>
public class ProfileViewModel
{
    /// <summary>
    /// Unikalny identyfikator użytkownika.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Adres email użytkownika.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Status weryfikacji adresu email.
    /// </summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// Identyfikator IANA strefy czasowej użytkownika (np. "Europe/Warsaw").
    /// </summary>
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>
    /// Data i czas utworzenia konta w UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Liczba nawyków użytkownika.
    /// </summary>
    public int HabitsCount { get; set; }
}
