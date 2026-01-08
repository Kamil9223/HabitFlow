using System.Net;
using HabitFlow.Tests.IntegrationTests.Infrastructure;
using Xunit;

namespace HabitFlow.Tests.IntegrationTests.Auth;

public class LogoutEndpointTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task Logout_WithoutAuthentication_ReturnsUnauthorized()
    {
        if (!fixture.IsAvailable)
            return;

        using var client = fixture.CreateClient();

        var response = await client.PostAsync("/api/v1/auth/logout", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
