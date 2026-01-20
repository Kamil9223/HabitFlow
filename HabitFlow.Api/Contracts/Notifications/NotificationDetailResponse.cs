namespace HabitFlow.Api.Contracts.Notifications;

public record NotificationDetailResponse(
    long Id,
    int HabitId,
    string HabitName,
    DateOnly LocalDate,
    int Type,
    string Content,
    int? AiStatus,
    DateTimeOffset CreatedAtUtc
);
