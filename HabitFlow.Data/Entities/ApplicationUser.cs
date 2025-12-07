using Microsoft.AspNetCore.Identity;

namespace HabitFlow.Data.Entities;

/// <summary>
/// Extension of ASP.NET Core Identity user with domain-specific fields.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's timezone (IANA format, e.g., "Europe/Warsaw").
    /// Used for calculating local dates for check-ins and notifications.
    /// </summary>
    public string TimeZoneId { get; set; } = null!;

    /// <summary>
    /// User account creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    // Navigation properties
    public ICollection<Habit> Habits { get; set; } = new List<Habit>();
    public ICollection<Checkin> Checkins { get; set; } = new List<Checkin>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
