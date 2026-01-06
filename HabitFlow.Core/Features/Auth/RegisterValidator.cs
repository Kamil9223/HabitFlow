using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using HabitFlow.Core.Common;

namespace HabitFlow.Core.Features.Auth;

public static partial class RegisterValidator
{
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 100;
    private const int MinDisplayNameLength = 2;
    private const int MaxDisplayNameLength = 50;
    private const int MaxEmailLength = 255;

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"[A-Z]")]
    private static partial Regex UppercaseRegex();

    [GeneratedRegex(@"[a-z]")]
    private static partial Regex LowercaseRegex();

    [GeneratedRegex(@"\d")]
    private static partial Regex DigitRegex();

    public static List<Error> Validate(RegisterCommand command)
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

        // Password validation
        if (string.IsNullOrWhiteSpace(command.Password))
        {
            errors.Add(Error.Validation("User.PasswordRequired", "Password is required."));
        }
        else
        {
            if (command.Password.Length < MinPasswordLength)
            {
                errors.Add(Error.Validation("User.PasswordTooShort",
                    $"Password must be at least {MinPasswordLength} characters long."));
            }

            if (command.Password.Length > MaxPasswordLength)
            {
                errors.Add(Error.Validation("User.PasswordTooLong",
                    $"Password must not exceed {MaxPasswordLength} characters."));
            }

            if (!UppercaseRegex().IsMatch(command.Password))
            {
                errors.Add(Error.Validation("User.PasswordMissingUppercase",
                    "Password must contain at least one uppercase letter."));
            }

            if (!LowercaseRegex().IsMatch(command.Password))
            {
                errors.Add(Error.Validation("User.PasswordMissingLowercase",
                    "Password must contain at least one lowercase letter."));
            }

            if (!DigitRegex().IsMatch(command.Password))
            {
                errors.Add(Error.Validation("User.PasswordMissingDigit",
                    "Password must contain at least one digit."));
            }
        }

        // DisplayName validation (optional field)
        if (!string.IsNullOrWhiteSpace(command.DisplayName))
        {
            if (command.DisplayName.Length < MinDisplayNameLength)
            {
                errors.Add(Error.Validation("User.DisplayNameTooShort",
                    $"Display name must be at least {MinDisplayNameLength} characters long."));
            }

            if (command.DisplayName.Length > MaxDisplayNameLength)
            {
                errors.Add(Error.Validation("User.DisplayNameTooLong",
                    $"Display name must not exceed {MaxDisplayNameLength} characters."));
            }
        }

        return errors;
    }
}
