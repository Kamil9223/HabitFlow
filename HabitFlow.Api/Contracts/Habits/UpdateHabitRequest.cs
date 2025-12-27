namespace HabitFlow.Api.Contracts.Habits;

public record UpdateHabitRequest(
    string? Title,
    string? Description,
    byte? Type,
    byte? CompletionMode,
    byte? DaysOfWeekMask,
    short? TargetValue,
    string? TargetUnit,
    DateOnly? DeadlineDate,
    bool? ClearDeadline = null
);
