using HabitFlow.Data.Enums;

namespace HabitFlow.Core.Abstractions.Notifications;

/// <summary>
/// Input context for notification content generation.
/// </summary>
public record NotificationContentContext(
    Guid UserId,
    int HabitId,
    string HabitName,
    int StreakDays,
    int TotalCompletions,
    int DaysSinceLastCompletion,
    double CompletionRate
);

/// <summary>
/// Result of content generation.
/// </summary>
public record NotificationContentResult(
    string Content,
    AiGenerationStatus Status,
    string? AiError
);

/// <summary>
/// Summary of a notification generation run.
/// </summary>
public record NotificationGenerationSummary(
    int HabitsProcessed,
    int NotificationsCreated,
    int Errors
);

/// <summary>
/// Request for LLM completion.
/// </summary>
public record LlmCompletionRequest(
    string SystemPrompt,
    string UserPrompt,
    int MaxTokens,
    double Temperature,
    TimeSpan Timeout
);
