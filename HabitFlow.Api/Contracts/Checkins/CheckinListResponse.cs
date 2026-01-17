namespace HabitFlow.Api.Contracts.Checkins;

public record CheckinListResponse(
    int HabitId,
    string From,
    string To,
    List<CheckinItemDto> Items
);

public record CheckinItemDto(
    long Id,
    string LocalDate,
    int ActualValue,
    short TargetValueSnapshot,
    byte CompletionModeSnapshot,
    byte HabitTypeSnapshot,
    bool IsPlanned
);
