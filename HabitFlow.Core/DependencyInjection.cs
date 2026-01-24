using System.Reflection;
using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Notifications;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Infrastructure;
using HabitFlow.Core.Infrastructure.Notifications;
using HabitFlow.Core.Options;
using HabitFlow.Core.Options.Validation;
using HabitFlow.Core.Services;
using HabitFlow.Core.Services.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using HabitFlow.Core.Options;

namespace HabitFlow.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddOptions<LlmSettings>()
            .Bind(configuration.GetSection(LlmSettings.SectionName));
        services.AddOptions<NotificationJobSettings>()
            .Bind(configuration.GetSection(NotificationJobSettings.SectionName));
        services.AddOptions<NotificationFeaturesOptions>()
            .Bind(configuration.GetSection(NotificationFeaturesOptions.SectionName));

        services.AddSingleton<IValidateOptions<LlmSettings>, LlmSettingsValidator>();
        services.AddSingleton<IValidateOptions<NotificationJobSettings>, NotificationJobSettingsValidator>();

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

        // Register application services
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<ILoggedUserContext, LoggedUserContext>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<FallbackContentGenerator>();
        services.AddScoped<INotificationContentGenerator, AiContentGenerator>();
        services.AddScoped<INotificationGenerationService, NotificationGenerationService>();
        services.AddHostedService<NotificationGenerationHostedService>();
        services.AddHttpClient<ILlmClient, OpenAiLlmClient>()
            .AddPolicyHandler((serviceProvider, _) =>
            {
                var settings = serviceProvider.GetRequiredService<IOptions<LlmSettings>>().Value;
                return CreateRetryPolicy(settings.MaxRetries);
            })
            .AddPolicyHandler(CreateCircuitBreakerPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(int maxRetries)
    {
        var retryCount = Math.Max(0, maxRetries);
        if (retryCount == 0)
            return Policy.NoOpAsync<HttpResponseMessage>();

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(retryCount, attempt => TimeSpan.FromSeconds(Math.Min(4, Math.Pow(2, attempt - 1))));
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(5, TimeSpan.FromMinutes(5));
}
