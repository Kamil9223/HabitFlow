namespace HabitFlow.Data.Enums;

/// <summary>
/// Represents the status of AI content generation for notifications.
/// </summary>
public enum AiGenerationStatus : byte
{
    /// <summary>
    /// AI successfully generated the content.
    /// </summary>
    Success = 1,

    /// <summary>
    /// AI failed, fallback template was used.
    /// </summary>
    Fallback = 2,

    /// <summary>
    /// AI generation encountered an error.
    /// </summary>
    Error = 3
}
