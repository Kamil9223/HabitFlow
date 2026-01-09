using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HabitFlow.Tests.IntegrationTests.Infrastructure;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public IntegrationTestFactory Factory { get; private set; } = null!;

    public HttpClient CreateClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    public async Task InitializeAsync()
    {
        await TestDatabase.EnsureStartedAsync();

        Factory = new IntegrationTestFactory(TestDatabase.ConnectionString);

        _ = Factory.Services;

        await TestDatabase.EnsureMigratedAsync(Factory.Services);
    }

    public Task DisposeAsync()
    {
        Factory?.Dispose();
        return Task.CompletedTask;
    }
}
