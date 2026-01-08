using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HabitFlow.Tests.IntegrationTests.Infrastructure;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public IntegrationTestFactory Factory { get; private set; } = null!;
    public bool IsAvailable { get; private set; }

    public HttpClient CreateClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    public async Task InitializeAsync()
    {
        try
        {
            await TestDatabase.EnsureStartedAsync();
            Factory = new IntegrationTestFactory(TestDatabase.ConnectionString);
            _ = Factory.Services;
            await TestDatabase.EnsureMigratedAsync(Factory.Services);
            IsAvailable = true;
        }
        catch (Exception ex) when (ex.Message.Contains("Docker is either not running", StringComparison.OrdinalIgnoreCase))
        {
            IsAvailable = false;
        }
    }

    public Task DisposeAsync()
    {
        Factory?.Dispose();
        return Task.CompletedTask;
    }
}
