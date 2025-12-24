namespace HabitFlow.Api.Contracts.Auth;

public record RegisterRequest(
    string Email,
    string Password
);
