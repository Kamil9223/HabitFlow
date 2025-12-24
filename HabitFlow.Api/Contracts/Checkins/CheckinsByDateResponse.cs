namespace HabitFlow.Api.Contracts.Checkins;

public record CheckinsByDateResponse(
    DateOnly Date,
    IReadOnlyList<CheckinsByDateItem> Items
);

public record CheckinsByDateItem(
    long Id,
    int HabitId,
    DateOnly LocalDate,
    int ActualValue,
    bool IsPlanned
);
