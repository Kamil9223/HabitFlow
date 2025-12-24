namespace HabitFlow.Core.Abstractions;

/// <summary>
/// Represents an error with a code, title, and description.
/// </summary>
public record Error(string Code, string Title, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty, string.Empty);

    public static Error Validation(string code, string description) =>
        new(code, "Validation Error", description);

    public static Error NotFound(string code, string description) =>
        new(code, "Not Found", description);

    public static Error Conflict(string code, string description) =>
        new(code, "Conflict", description);

    public static Error Failure(string code, string description) =>
        new(code, "Failure", description);
}
