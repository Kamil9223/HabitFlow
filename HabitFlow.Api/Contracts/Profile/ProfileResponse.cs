namespace HabitFlow.Api.Contracts.Profile;

public record ProfileResponse(
    string UserId,
    string Email,
    bool EmailConfirmed,
    string TimeZoneId,
    DateTimeOffset CreatedAtUtc
);
