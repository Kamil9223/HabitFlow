namespace HabitFlow.Core.Abstractions;

/// <summary>
/// Dispatches queries to their respective handlers.
/// </summary>
public interface IQueryDispatcher
{
    Task<TResult> Dispatch<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
}
