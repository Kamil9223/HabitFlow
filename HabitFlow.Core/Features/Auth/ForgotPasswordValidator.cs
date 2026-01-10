using System.Text.RegularExpressions;
using HabitFlow.Core.Common;

namespace HabitFlow.Core.Features.Auth;

public static partial class ForgotPasswordValidator
{
    private const int MaxEmailLength = 255;

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex EmailRegex();

    public static List<Error> Validate(ForgotPasswordCommand command)
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

        return errors;
    }
}
