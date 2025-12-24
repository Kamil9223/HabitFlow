namespace HabitFlow.Api.Contracts.Habits;

public record CreateHabitRequest(
    string Title,
    string? Description,
    int Type,
    int CompletionMode,
    int DaysOfWeekMask,
    int TargetValue,
    string? TargetUnit,
    DateOnly? DeadlineDate
);
