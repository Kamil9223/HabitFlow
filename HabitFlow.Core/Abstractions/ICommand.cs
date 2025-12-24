namespace HabitFlow.Core.Abstractions;

/// <summary>
/// Marker interface for commands.
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Marker interface for commands with a result.
/// </summary>
public interface ICommand<TResult> : ICommand
{
}
