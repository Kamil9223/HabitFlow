namespace HabitFlow.Api.Contracts.Auth;

public record RefreshResponse(
    string AccessToken,
    int ExpiresIn,
    string? RefreshToken
);
