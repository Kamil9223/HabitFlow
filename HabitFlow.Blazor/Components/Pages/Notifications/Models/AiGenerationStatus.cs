namespace HabitFlow.Blazor.Components.Pages.Notifications.Models;

/// <summary>
/// Status generowania treści przez AI (zsynchronizowany z HabitFlow.Data.Enums.AiGenerationStatus).
/// </summary>
public enum AiGenerationStatus : byte
{
    /// <summary>
    /// AI pomyślnie wygenerowało treść.
    /// </summary>
    Success = 1,

    /// <summary>
    /// AI zawiodło, użyto szablonu fallback.
    /// </summary>
    Fallback = 2,

    /// <summary>
    /// Błąd generowania przez AI.
    /// </summary>
    Error = 3
}
