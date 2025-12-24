namespace HabitFlow.Api.Contracts.Auth;

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword
);
