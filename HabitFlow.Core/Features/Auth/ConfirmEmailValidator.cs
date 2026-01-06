using HabitFlow.Core.Common;

namespace HabitFlow.Core.Features.Auth;

public static class ConfirmEmailValidator
{
    public static List<Error> Validate(ConfirmEmailCommand command)
    {
        var errors = new List<Error>();

        // UserId validation
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            errors.Add(Error.Validation("ConfirmEmail.UserIdRequired", "User ID is required."));
        }

        // Token validation
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            errors.Add(Error.Validation("ConfirmEmail.TokenRequired", "Confirmation token is required."));
        }

        return errors;
    }
}
