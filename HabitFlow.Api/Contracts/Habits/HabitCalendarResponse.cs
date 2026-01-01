using HabitFlow.Data.Enums;

namespace HabitFlow.Api.Contracts.Habits;

public record HabitCalendarResponse(
    int HabitId,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<HabitCalendarDay> Days
);

public record HabitCalendarDay(
    DateOnly Date,
    bool IsPlanned,
    int ActualValue,
    int? TargetValueSnapshot,
    CompletionMode? CompletionModeSnapshot,
    HabitType? HabitTypeSnapshot,
    double DailyScore
);
