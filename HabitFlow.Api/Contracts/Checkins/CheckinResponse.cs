using HabitFlow.Data.Enums;

namespace HabitFlow.Api.Contracts.Checkins;

public record CheckinResponse(
    long Id,
    int HabitId,
    DateOnly LocalDate,
    int ActualValue,
    short TargetValueSnapshot,
    CompletionMode CompletionModeSnapshot,
    HabitType HabitTypeSnapshot,
    bool IsPlanned,
    DateTimeOffset CreatedAtUtc
);
