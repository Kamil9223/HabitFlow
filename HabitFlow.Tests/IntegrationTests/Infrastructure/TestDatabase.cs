using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Microsoft.Extensions.DependencyInjection;
using HabitFlow.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace HabitFlow.Tests.IntegrationTests.Infrastructure;

internal static class TestDatabase
{
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private static readonly SemaphoreSlim MigrateLock = new(1, 1);
    private static MsSqlContainer? _container;
    private static bool _initialized;
    private static bool _migrated;

    public static string ConnectionString =>
        _container?.GetConnectionString() ?? throw new InvalidOperationException("Test database not initialized.");

    public static async Task EnsureStartedAsync()
    {
        if (_initialized)
            return;

        await InitLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            _container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("SQL Server is now ready for client connections"))
                .Build();

            await _container.StartAsync();
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
