namespace HabitFlow.Api.Contracts.Auth;

public record RegisterResponse(
    string UserId,
    string Email,
    bool EmailConfirmed
);
