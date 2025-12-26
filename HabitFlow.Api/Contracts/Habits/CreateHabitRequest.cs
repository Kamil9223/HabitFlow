namespace HabitFlow.Api.Contracts.Habits;

public record CreateHabitRequest(
    string Title,
    string? Description,
    byte Type,
    byte CompletionMode,
    byte DaysOfWeekMask,
    short TargetValue,
    string? TargetUnit,
    DateOnly? DeadlineDate
);
