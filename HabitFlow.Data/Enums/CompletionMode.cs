namespace HabitFlow.Data.Enums;

/// <summary>
/// Represents how habit progress is tracked and measured.
/// </summary>
public enum CompletionMode : byte
{
    /// <summary>
    /// Binary completion: done (1) or not done (0).
    /// </summary>
    Binary = 1,

    /// <summary>
    /// Quantitative: track numeric progress (e.g., pages read, meals).
    /// </summary>
    Quantitative = 2
}
