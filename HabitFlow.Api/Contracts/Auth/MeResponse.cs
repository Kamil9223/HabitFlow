namespace HabitFlow.Api.Contracts.Auth;

public record MeResponse(
    string UserId,
    string Email,
    bool EmailConfirmed,
    string TimeZoneId,
    DateTimeOffset CreatedAtUtc
);
