using HabitFlow.Core.Common;
using System.Text.RegularExpressions;

namespace HabitFlow.Core.Features.Auth;

public static partial class LoginValidator
{
    private const int MinPasswordLength = 8;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    public static List<Error> Validate(LoginCommand command)
    {
        var errors = new List<Error>();

        // Email validation
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            errors.Add(Error.Validation("Login.EmailRequired", "Email is required."));
        }
        else if (!EmailRegex().IsMatch(command.Email))
        {
            errors.Add(Error.Validation("Login.EmailInvalid", "Email format is invalid."));
        }

        // Password validation
        if (string.IsNullOrWhiteSpace(command.Password))
        {
            errors.Add(Error.Validation("Login.PasswordRequired", "Password is required."));
        }
        else if (command.Password.Length < MinPasswordLength)
        {
            errors.Add(Error.Validation("Login.PasswordTooShort",
                $"Password must be at least {MinPasswordLength} characters long."));
        }

        return errors;
    }
}
