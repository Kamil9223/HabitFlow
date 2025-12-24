namespace HabitFlow.Core.Abstractions;

/// <summary>
/// Dispatcher for sending commands to their handlers.
/// </summary>
public interface ICommandDispatcher
{
    Task<TResult> Dispatch<TResult>(ICommand<TResult> command, CancellationToken cancellationToken);
}
