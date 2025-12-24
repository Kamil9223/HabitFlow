namespace HabitFlow.Api.Contracts.Progress;

public record ProgressRollingResponse(
    int HabitId,
    int WindowDays,
    DateOnly Until,
    IReadOnlyList<ProgressRollingPoint> Points
);

public record ProgressRollingPoint(
    DateOnly Date,
    int PlannedDays,
    double SumDailyScore,
    double SuccessRate
);
