namespace HabitFlow.Api.Contracts.Checkins;

public record CheckinResponse(
    long Id,
    int HabitId,
    string UserId,
    DateOnly LocalDate,
    int ActualValue,
    int TargetValueSnapshot,
    int CompletionModeSnapshot,
    int HabitTypeSnapshot,
    bool IsPlanned,
    DateTimeOffset CreatedAtUtc
);
