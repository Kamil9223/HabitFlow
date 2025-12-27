using HabitFlow.Core.Abstractions;

namespace HabitFlow.Core.Infrastructure;

public class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public async Task<TResult> Dispatch<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for query {query.GetType().Name}");

        var handleMethod = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.Handle))
            ?? throw new InvalidOperationException($"Handle method not found on handler for {query.GetType().Name}");

        var result = await (Task<TResult>)handleMethod.Invoke(handler, [query, cancellationToken])!;
        return result;
    }
}
