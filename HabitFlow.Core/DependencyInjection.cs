using System.Reflection;
using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HabitFlow.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register Command Dispatcher
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();

        // Auto-register all command handlers from this assembly
        var commandHandlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .SelectMany(t => t.GetInterfaces(), (type, iface) => new { type, iface })
            .Where(x => x.iface.IsGenericType && x.iface.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))
            .Select(x => new { Implementation = x.type, Interface = x.iface });

        foreach (var handler in commandHandlerTypes)
        {
            services.AddScoped(handler.Interface, handler.Implementation);
        }

        // Register Query Dispatcher
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // Auto-register all query handlers from this assembly
        var queryHandlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .SelectMany(t => t.GetInterfaces(), (type, iface) => new { type, iface })
            .Where(x => x.iface.IsGenericType && x.iface.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
            .Select(x => new { Implementation = x.type, Interface = x.iface });

        foreach (var handler in queryHandlerTypes)
        {
            services.AddScoped(handler.Interface, handler.Implementation);
        }

        return services;
    }
}
