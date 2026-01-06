namespace HabitFlow.Api.Contracts.Auth;

public record LoginResponse(
    string UserId,
    string Email,
    bool EmailConfirmed
);
