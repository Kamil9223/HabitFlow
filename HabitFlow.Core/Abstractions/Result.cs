namespace HabitFlow.Core.Abstractions;

/// <summary>
/// Represents the result of an operation without a value.
/// </summary>
public class Result
{
    private readonly List<Error> _errors = new();

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<Error> Errors => _errors;
    
    public Error Error => _errors.Count > 0 ? _errors[0] : Error.None;

    protected Result(bool isSuccess, IEnumerable<Error>? errors = null)
    {
        IsSuccess = isSuccess;

        if (errors != null)
            _errors.AddRange(errors);

        if (isSuccess && _errors.Count > 0)
            throw new InvalidOperationException("Success result cannot have errors.");

        if (!isSuccess && _errors.Count == 0)
            throw new InvalidOperationException("Failure result must have at least one error.");
    }

    public static Result Success() => new(true);

    public static Result Failure(Error error) => new(false, new[] { error });

    public static Result Failure(IEnumerable<Error> errors) => new(false, errors);

    public static Result<T> Success<T>(T value) => new(value, true);

    public static Result<T> Failure<T>(Error error) => new(default!, false, new[] { error });

    public static Result<T> Failure<T>(IEnumerable<Error> errors) => new(default!, false, errors);
}

public class Result<T> : Result
{
    private readonly T? _value;

    protected internal Result(T? value, bool isSuccess, IEnumerable<Error>? errors = null)
        : base(isSuccess, errors)
        => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");
}
