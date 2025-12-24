namespace HabitFlow.Api.Contracts.Notifications;

public record NotificationResponse(
    long Id,
    int HabitId,
    DateOnly LocalDate,
    int Type,
    string Content,
    int? AiStatus,
    DateTimeOffset CreatedAtUtc
);
