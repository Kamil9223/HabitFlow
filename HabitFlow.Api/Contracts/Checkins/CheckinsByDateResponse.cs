namespace HabitFlow.Api.Contracts.Checkins;

public record CheckinsByDateResponse(
    IReadOnlyList<CheckinsByDateItem> Items
);

public record CheckinsByDateItem(
    long Id,
    int HabitId,
    DateOnly LocalDate,
    int ActualValue,
    bool IsPlanned
);
