using HabitFlow.Core.Abstractions;

namespace HabitFlow.Core.Infrastructure;

public class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    public async Task<TResult> Dispatch<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for command {command.GetType().Name}");

        var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.Handle))
            ?? throw new InvalidOperationException($"Handle method not found on handler for {command.GetType().Name}");

        var result = await (Task<TResult>)handleMethod.Invoke(handler, [command, cancellationToken])!;
        return result;
    }
}
