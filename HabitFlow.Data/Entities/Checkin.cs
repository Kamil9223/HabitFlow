namespace HabitFlow.Data.Entities;

/// <summary>
/// Represents a daily user check-in for a habit.
/// </summary>
public class Checkin
{
    public long Id { get; set; }

    /// <summary>
    /// Associated habit identifier.
    /// </summary>
    public int HabitId { get; set; }

    /// <summary>
    /// User identifier (denormalized for RLS and performance).
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Local check-in date (according to user's timezone).
    /// </summary>
    public DateOnly LocalDate { get; set; }

    /// <summary>
    /// Actual value for the day (e.g., pages read, number of meals, number of violations).
    /// </summary>
    public int ActualValue { get; set; }

    /// <summary>
    /// Snapshot of Habits.TargetValue at check-in time.
    /// </summary>
    public short TargetValueSnapshot { get; set; }

    /// <summary>
    /// Snapshot of CompletionMode at check-in time (1=Binary, 2=Quantitative, 3=Checklist).
    /// </summary>
    public byte CompletionModeSnapshot { get; set; }

    /// <summary>
    /// Snapshot of Type at check-in time (1=Start, 2=Stop).
    /// </summary>
    public byte HabitTypeSnapshot { get; set; }

    /// <summary>
    /// Whether the day was planned (according to DaysOfWeekMask).
    /// </summary>
    public bool IsPlanned { get; set; }

    /// <summary>
    /// Check-in creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    // Navigation properties
    public Habit Habit { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
