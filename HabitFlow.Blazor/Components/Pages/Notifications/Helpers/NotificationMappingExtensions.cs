using HabitFlow.Blazor.Components.Pages.Notifications.Models;
using HabitFlow.Client;

namespace HabitFlow.Blazor.Components.Pages.Notifications.Helpers;

/// <summary>
/// Metody rozszerzające do mapowania między DTO API a modelami widoku powiadomień.
/// </summary>
public static class NotificationMappingExtensions
{
    /// <summary>
    /// Mapuje NotificationResponse z API na NotificationListItemVm.
    /// </summary>
    public static NotificationListItemVm ToListItemVm(this NotificationResponse response)
    {
        return new NotificationListItemVm
        {
            Id = response.Id,
            HabitId = response.HabitId,
            HabitTitle = null, // Cache opcjonalny w rozbudowie
            LocalDate = response.LocalDate,
            Type = (NotificationType)response.Type,
            TypeLabel = GetTypeLabel((NotificationType)response.Type),
            Content = response.Content,
            AiStatus = response.AiStatus.HasValue
                ? (AiGenerationStatus)response.AiStatus.Value
                : null,
            AiStatusLabel = response.AiStatus.HasValue
                ? GetAiStatusLabel((AiGenerationStatus)response.AiStatus.Value)
                : null,
            CreatedAtUtc = response.CreatedAtUtc
        };
    }

    /// <summary>
    /// Zwraca label dla typu powiadomienia.
    /// </summary>
    private static string GetTypeLabel(NotificationType type) => type switch
    {
        NotificationType.MissDue => "Miss Due",
        _ => type.ToString()
    };

    /// <summary>
    /// Zwraca label dla statusu AI.
    /// </summary>
    private static string GetAiStatusLabel(AiGenerationStatus status) => status switch
    {
        AiGenerationStatus.Success => "Sukces AI",
        AiGenerationStatus.Fallback => "Szablon",
        AiGenerationStatus.Error => "Błąd AI",
        _ => status.ToString()
    };
}
