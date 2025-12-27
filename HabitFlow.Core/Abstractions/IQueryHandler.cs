namespace HabitFlow.Core.Abstractions;

/// <summary>
/// Defines a handler for a query.
/// </summary>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);
}
