namespace HabitFlow.Api.Contracts.Checkins;

public record CreateCheckinRequest(
    DateOnly LocalDate,
    int ActualValue
);
