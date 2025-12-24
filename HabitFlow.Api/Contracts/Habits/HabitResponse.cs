namespace HabitFlow.Api.Contracts.Habits;

public record HabitResponse(
    int Id,
    string Title,
    string? Description,
    int Type,
    int CompletionMode,
    int DaysOfWeekMask,
    int TargetValue,
    string? TargetUnit,
    DateOnly? DeadlineDate,
    DateTimeOffset CreatedAtUtc
);
