namespace HabitFlow.Core.Common;

public static class ErrorTitles
{
    public const string ValidationError = "Validation Error";
    public const string NotFound = "Not Found";
    public const string Conflict = "Conflict";
    public const string Unauthorized = "Unauthorized";
    public const string Forbidden = "Forbidden";
    public const string UnprocessableEntity = "Unprocessable Entity";
}

/// <summary>
/// Represents an error with a code, title, and description.
/// </summary>
public record Error(string Code, string Title, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty, string.Empty);

    public static Error Validation(string code, string description) =>
        new(code, ErrorTitles.ValidationError, description);

    public static Error NotFound(string code, string description) =>
        new(code, ErrorTitles.NotFound, description);

    public static Error Conflict(string code, string description) =>
        new(code, ErrorTitles.Conflict, description);

    public static Error Unauthorized(string code, string description) =>
        new(code, ErrorTitles.Unauthorized, description);

    public static Error Forbidden(string code, string description) =>
        new(code, ErrorTitles.Forbidden, description);

    public static Error UnprocessableEntity(string code, string description) =>
        new(code, ErrorTitles.UnprocessableEntity, description);

    public static Error Failure(string code, string description) =>
        new(code, "Failure", description);
}
