namespace HabitFlow.Data.Entities;

/// <summary>
/// Represents a system-generated notification.
/// </summary>
public class Notification
{
    public long Id { get; set; }

    /// <summary>
    /// Notification recipient identifier.
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Associated habit identifier.
    /// </summary>
    public int HabitId { get; set; }

    /// <summary>
    /// Local date related to the notification.
    /// </summary>
    public DateOnly LocalDate { get; set; }

    /// <summary>
    /// Notification type: 1 = MissDue.
    /// </summary>
    public byte Type { get; set; }

    /// <summary>
    /// Notification content (max 1024 characters).
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// AI generation status: 1=Success, 2=Fallback, 3=Error.
    /// </summary>
    public byte? AiStatus { get; set; }

    /// <summary>
    /// AI error description (max 512 characters, for diagnostic purposes).
    /// </summary>
    public string? AiError { get; set; }

    /// <summary>
    /// Notification creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public Habit Habit { get; set; } = null!;
}
