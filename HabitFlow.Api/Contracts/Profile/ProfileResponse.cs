namespace HabitFlow.Api.Contracts.Profile;

public record ProfileResponse(
    Guid UserId,
    string Email,
    bool EmailConfirmed,
    string TimeZoneId,
    DateTimeOffset CreatedAtUtc,
    int HabitsCount
);
