namespace HabitFlow.Core.Abstractions;

/// <summary>
/// Handler for commands that return a result.
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken);
}
