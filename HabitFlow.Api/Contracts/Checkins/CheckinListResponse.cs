namespace HabitFlow.Api.Contracts.Checkins;

public record CheckinListResponse(
    int HabitId,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<CheckinListItem> Items
);

public record CheckinListItem(
    long Id,
    DateOnly LocalDate,
    int ActualValue,
    int TargetValueSnapshot,
    int CompletionModeSnapshot,
    int HabitTypeSnapshot,
    bool IsPlanned
);
