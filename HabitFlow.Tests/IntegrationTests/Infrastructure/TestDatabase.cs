using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Microsoft.Extensions.DependencyInjection;
using HabitFlow.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using DotNet.Testcontainers.Containers;

namespace HabitFlow.Tests.IntegrationTests.Infrastructure;

internal static class TestDatabase
{
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private static readonly SemaphoreSlim MigrateLock = new(1, 1);
    private static MsSqlContainer? _container;
    private static IContainer? _smtpContainer;
    private static bool _initialized;
    private static bool _migrated;

    public static string ConnectionString =>
        _container?.GetConnectionString() ?? throw new InvalidOperationException("Test database not initialized.");

    public static string SmtpHost =>
        _smtpContainer?.Hostname ?? throw new InvalidOperationException("SMTP container not initialized.");

    public static int SmtpPort =>
        _smtpContainer?.GetMappedPublicPort(1025) ?? throw new InvalidOperationException("SMTP container not initialized.");

    public static async Task EnsureStartedAsync()
    {
        if (_initialized)
            return;

        await InitLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            // Start SQL Server container
            _container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("SQL Server is now ready for client connections"))
                .Build();

            // Start MailHog SMTP container for email testing
            _smtpContainer = new ContainerBuilder()
                .WithImage("mailhog/mailhog:latest")
                .WithPortBinding(1025, true) // SMTP port
                .WithPortBinding(8025, true) // HTTP API port
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1025))
                .Build();

            // Start both containers
            await Task.WhenAll(
                _container.StartAsync(),
                _smtpContainer.StartAsync()
            );

            _initialized = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    public static async Task EnsureMigratedAsync(IServiceProvider services)
    {
        if (_migrated)
            return;

        await MigrateLock.WaitAsync();
        try
        {
            if (_migrated)
                return;

            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<HabitFlowDbContext>();
            await dbContext.Database.MigrateAsync();
            _migrated = true;
        }
        finally
        {
            MigrateLock.Release();
        }
    }
}
