namespace HabitFlow.Api.Contracts.Auth;

public record LoginResponse(
    Guid UserId,
    string Email,
    bool EmailConfirmed
);
