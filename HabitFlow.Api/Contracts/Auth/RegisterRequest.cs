using System.ComponentModel.DataAnnotations;

namespace HabitFlow.Api.Contracts.Auth;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(255)]
    string Email,

    [Required, MinLength(8), MaxLength(100)]
    string Password,

    [MaxLength(50)]
    string? DisplayName
);
