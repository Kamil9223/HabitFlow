using HabitFlow.Data.Enums;

namespace HabitFlow.Api.Contracts.Habits;

public record HabitResponse(
    int Id,
    string Title,
    string? Description,
    HabitType Type,
    CompletionMode CompletionMode,
    int DaysOfWeekMask,
    int TargetValue,
    string? TargetUnit,
    DateOnly? DeadlineDate,
    DateTimeOffset CreatedAtUtc
);
