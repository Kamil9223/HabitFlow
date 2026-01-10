namespace HabitFlow.Api.Contracts.Auth;

public record RegisterResponse(
    Guid UserId,
    string Email,
    bool EmailConfirmed
);
