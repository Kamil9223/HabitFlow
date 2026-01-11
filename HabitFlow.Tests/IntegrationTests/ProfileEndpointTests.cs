using System.Net;
using System.Net.Http.Json;
using HabitFlow.Api.Contracts.Auth;
using HabitFlow.Api.Contracts.Profile;
using HabitFlow.Data.Entities;
using HabitFlow.Tests.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HabitFlow.Tests.IntegrationTests;

public class ProfileEndpointTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task GetProfile_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/profile");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithAuthentication_ReturnsUserData()
    {
        // Arrange: Create and confirm a user
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"profile-test-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";
        var createdAt = DateTime.UtcNow;

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true,
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = createdAt
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        // Act: Login and call /profile
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

        var profileResponse = await client.GetAsync("/api/v1/profile");

        // Assert
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var profileData = await profileResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(profileData);
        Assert.Equal(user.Id, profileData.UserId);
        Assert.Equal(testEmail, profileData.Email);
        Assert.True(profileData.EmailConfirmed);
        Assert.Equal("Europe/Warsaw", profileData.TimeZoneId);
        Assert.Equal(createdAt.Date, profileData.CreatedAtUtc.Date);
        Assert.Equal(0, profileData.HabitsCount);
    }

    [Fact]
    public async Task GetProfile_WithUnconfirmedEmail_ReturnsUserDataWithCorrectFlag()
    {
        // Arrange: Create user with unconfirmed email
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"profile-unconfirmed-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = false, // Not confirmed
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        // Manually confirm email to allow login
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);

        // Login
        using var client = fixture.CreateClient();
        var loginRequest = new LoginRequest(testEmail, testPassword);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        foreach (var cookie in cookies)
        {
            client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        // Now set EmailConfirmed back to false to test the response
        user.EmailConfirmed = false;
        await userManager.UpdateAsync(user);

        // Act
        var profileResponse = await client.GetAsync("/api/v1/profile");

        // Assert
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        var profileData = await profileResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(profileData);
        Assert.False(profileData.EmailConfirmed);
    }

    [Fact]
    public async Task GetProfile_WithDifferentTimeZone_ReturnsMappedTimeZone()
    {
        // Arrange: Create user with different timezone
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"profile-timezone-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true,
            TimeZoneId = "America/New_York",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        // Login
        using var client = fixture.CreateClient();
        var loginRequest = new LoginRequest(testEmail, testPassword);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        foreach (var cookie in cookies)
        {
            client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        // Act
        var profileResponse = await client.GetAsync("/api/v1/profile");

        // Assert
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        var profileData = await profileResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(profileData);
        Assert.Equal("America/New_York", profileData.TimeZoneId);
    }

    [Fact]
    public async Task UpdateTimeZone_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var client = fixture.CreateClient();

        var request = new UpdateTimeZoneRequest("UTC");
        var response = await client.PatchAsJsonAsync("/api/v1/profile/timezone", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTimeZone_WithValidTimeZone_UpdatesSuccessfully()
    {
        // Arrange: Create and login a user
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"update-timezone-{Guid.NewGuid()}@test.com";
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

        // Login
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

        // Act: Update timezone
        var updateRequest = new UpdateTimeZoneRequest("America/New_York");
        var updateResponse = await client.PatchAsJsonAsync("/api/v1/profile/timezone", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        // Verify: Fetch profile to confirm timezone was updated
        var profileResponse = await client.GetAsync("/api/v1/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var profileData = await profileResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(profileData);
        Assert.Equal("America/New_York", profileData.TimeZoneId);
    }

    [Fact]
    public async Task UpdateTimeZone_WithInvalidTimeZone_ReturnsBadRequest()
    {
        // Arrange: Create and login a user
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"update-invalid-tz-{Guid.NewGuid()}@test.com";
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

        // Login
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

        // Act: Try to update with invalid timezone
        var updateRequest = new UpdateTimeZoneRequest("Invalid/TimeZone");
        var updateResponse = await client.PatchAsJsonAsync("/api/v1/profile/timezone", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);

        // Verify: Original timezone is unchanged
        var profileResponse = await client.GetAsync("/api/v1/profile");
        var profileData = await profileResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(profileData);
        Assert.Equal("Europe/Warsaw", profileData.TimeZoneId);
    }

    [Fact]
    public async Task UpdateTimeZone_WithEmptyTimeZone_ReturnsBadRequest()
    {
        // Arrange: Create and login a user
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"update-empty-tz-{Guid.NewGuid()}@test.com";
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

        // Login
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

        // Act: Try to update with empty timezone
        var updateRequest = new UpdateTimeZoneRequest("");
        var updateResponse = await client.PatchAsJsonAsync("/api/v1/profile/timezone", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateTimeZone_WithDifferentIANAFormats_UpdatesCorrectly()
    {
        // Arrange: Create and login a user
        using var scope = fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"update-iana-formats-{Guid.NewGuid()}@test.com";
        var testPassword = "Test123!";

        var user = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, testPassword);
        Assert.True(createResult.Succeeded);

        // Login
        using var client = fixture.CreateClient();
        var loginRequest = new LoginRequest(testEmail, testPassword);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Extract cookies
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        foreach (var cookie in cookies)
        {
            client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        // Act & Assert: Test various valid IANA timezones
        var validTimeZones = new[] { "UTC", "Europe/London", "Asia/Tokyo", "Australia/Sydney" };

        foreach (var timeZone in validTimeZones)
        {
            var updateRequest = new UpdateTimeZoneRequest(timeZone);
            var updateResponse = await client.PatchAsJsonAsync("/api/v1/profile/timezone", updateRequest);

            Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

            // Verify update
            var profileResponse = await client.GetAsync("/api/v1/profile");
            var profileData = await profileResponse.Content.ReadFromJsonAsync<ProfileResponse>();
            Assert.NotNull(profileData);
            Assert.Equal(timeZone, profileData.TimeZoneId);
        }
    }
}
