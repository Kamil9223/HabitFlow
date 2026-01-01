namespace HabitFlow.Data.Enums;

/// <summary>
/// Represents the type of habit.
/// </summary>
public enum HabitType : byte
{
    /// <summary>
    /// Start doing something (e.g., read daily, exercise).
    /// </summary>
    Start = 1,

    /// <summary>
    /// Stop doing something (e.g., quit smoking, reduce screen time).
    /// </summary>
    Stop = 2
}
