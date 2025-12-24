namespace HabitFlow.Api.Contracts.Auth;

public record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    string? RefreshToken
);
