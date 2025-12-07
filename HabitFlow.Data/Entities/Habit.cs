namespace HabitFlow.Data.Entities;

/// <summary>
/// Represents a user habit.
/// </summary>
public class Habit
{
    public int Id { get; set; }

    /// <summary>
    /// Identifier of the habit owner.
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Habit title (max 80 characters).
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Habit description (max 1000 characters).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Habit type: 1 = Start (begin doing), 2 = Stop (quit doing).
    /// </summary>
    public byte Type { get; set; }

    /// <summary>
    /// Completion mode: 1 = Binary, 2 = Quantitative, 3 = Checklist.
    /// </summary>
    public byte CompletionMode { get; set; }

    /// <summary>
    /// Days of week bitmask (bit 0=Mon, ..., bit 6=Sun). Value 1-127.
    /// </summary>
    public byte DaysOfWeekMask { get; set; }

    /// <summary>
    /// Target value per day (1-1000), e.g., number of pages, meals.
    /// </summary>
    public short TargetValue { get; set; }

    /// <summary>
    /// Target unit (e.g., 'pages', 'meals', 'tasks') - descriptive field for UI.
    /// </summary>
    public string? TargetUnit { get; set; }

    /// <summary>
    /// Optional habit deadline date.
    /// </summary>
    public DateOnly? DeadlineDate { get; set; }

    /// <summary>
    /// Habit creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
    public ICollection<Checkin> Checkins { get; set; } = new List<Checkin>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
