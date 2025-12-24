namespace HabitFlow.Api.Contracts.Today;

public record TodayResponse(
    DateOnly Date,
    IReadOnlyList<TodayItem> Items
);

public record TodayItem(
    int HabitId,
    string Title,
    int Type,
    int CompletionMode,
    int TargetValue,
    string? TargetUnit,
    bool IsPlanned,
    bool HasCheckin
);
