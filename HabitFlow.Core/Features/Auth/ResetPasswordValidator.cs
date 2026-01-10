using System.Text.RegularExpressions;
using HabitFlow.Core.Common;

namespace HabitFlow.Core.Features.Auth;

public static partial class ResetPasswordValidator
{
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 100;
    private const int MaxEmailLength = 255;

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"[A-Z]")]
    private static partial Regex UppercaseRegex();

    [GeneratedRegex(@"[a-z]")]
    private static partial Regex LowercaseRegex();

    [GeneratedRegex(@"\d")]
    private static partial Regex DigitRegex();

    public static List<Error> Validate(ResetPasswordCommand command)
    {
        var errors = new List<Error>();

        // Email validation
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            errors.Add(Error.Validation("User.EmailRequired", "Email is required."));
        }
        else
        {
            if (command.Email.Length > MaxEmailLength)
            {
                errors.Add(Error.Validation("User.EmailTooLong",
                    $"Email must not exceed {MaxEmailLength} characters."));
            }

            if (!EmailRegex().IsMatch(command.Email))
            {
                errors.Add(Error.Validation("User.EmailInvalidFormat",
                    "Email address format is invalid."));
            }
        }

        // Token validation
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            errors.Add(Error.Validation("Auth.TokenRequired", "Reset token is required."));
        }

        // Password validation
        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            errors.Add(Error.Validation("User.PasswordRequired", "Password is required."));
        }
        else
        {
            if (command.NewPassword.Length < MinPasswordLength)
            {
                errors.Add(Error.Validation("User.PasswordTooShort",
                    $"Password must be at least {MinPasswordLength} characters long."));
            }

            if (command.NewPassword.Length > MaxPasswordLength)
            {
                errors.Add(Error.Validation("User.PasswordTooLong",
                    $"Password must not exceed {MaxPasswordLength} characters."));
            }

            if (!UppercaseRegex().IsMatch(command.NewPassword))
            {
                errors.Add(Error.Validation("User.PasswordMissingUppercase",
                    "Password must contain at least one uppercase letter."));
            }

            if (!LowercaseRegex().IsMatch(command.NewPassword))
            {
                errors.Add(Error.Validation("User.PasswordMissingLowercase",
                    "Password must contain at least one lowercase letter."));
            }

            if (!DigitRegex().IsMatch(command.NewPassword))
            {
                errors.Add(Error.Validation("User.PasswordMissingDigit",
                    "Password must contain at least one digit."));
            }
        }

        return errors;
    }
}
