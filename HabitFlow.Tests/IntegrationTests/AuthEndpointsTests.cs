using System.Net;
using System.Net.Http.Json;
using HabitFlow.Api.Contracts.Auth;
using HabitFlow.Data.Entities;
using HabitFlow.Tests.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HabitFlow.Tests.IntegrationTests;

public class AuthEndpointsTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task Logout_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = fixture.CreateClient();

        var response = await client.PostAsync("/api/v1/auth/logout", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginAndLogout_Succeed()
    {
        // Arrange: Create and confirm a user
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"logout-test-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true,
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        // Act: Login and then logout
        using var client = fixture.CreateClient();

        var loginRequest = new LoginRequest(testEmail, testPassword);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Extract cookies from login response
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        foreach (var cookie in cookies)
        {
            client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", new StringContent(string.Empty));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
    }
}
