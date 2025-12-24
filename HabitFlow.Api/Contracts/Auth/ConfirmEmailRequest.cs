namespace HabitFlow.Api.Contracts.Auth;

public record ConfirmEmailRequest(
    string UserId,
    string Token
);
