using HabitFlow.Data.Enums;

namespace HabitFlow.Api.Contracts.Habits;

public record CreateHabitRequest(
    string Title,
    string? Description,
    HabitType Type,
    CompletionMode CompletionMode,
    byte DaysOfWeekMask,
    short TargetValue,
    string? TargetUnit,
    DateOnly? DeadlineDate
);
