using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HabitFlow.Api.Contracts.Auth;
using HabitFlow.Api.Contracts.Checkins;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using HabitFlow.Tests.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HabitFlow.Tests.IntegrationTests;

public class CheckinEndpointsTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly JsonSerializerOptions _options;

    public CheckinEndpointsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _options.Converters.Add(new JsonStringEnumConverter());
    }
    
    [Fact]
    public async Task CreateCheckin_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _fixture.CreateClient();
        var request = new CreateCheckinRequest(DateOnly.FromDateTime(DateTime.UtcNow), 5);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/habits/1/checkins", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCheckin_ValidRequest_Returns201Created()
    {
        // Arrange: Create user and habit
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        var request = new CreateCheckinRequest(
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: 7);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var checkin = await response.Content.ReadFromJsonAsync<CheckinResponse>(_options);
        Assert.NotNull(checkin);
        Assert.True(checkin.Id > 0);
        Assert.Equal(habitId, checkin.HabitId);
        Assert.Equal(7, checkin.ActualValue);
        Assert.Equal(10, checkin.TargetValueSnapshot);
        Assert.True(checkin.IsPlanned);
    }

    [Fact]
    public async Task CreateCheckin_HabitNotFound_Returns404()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();
        var request = new CreateCheckinRequest(DateOnly.FromDateTime(DateTime.UtcNow), 5);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/habits/999999/checkins", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCheckin_NotOwner_Returns403()
    {
        // Arrange: Create two users and habit for first user
        var (client1, userId1) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId1);

        // Create second user and authenticate
        var (client2, _) = await CreateAuthenticatedClientAsync();

        var request = new CreateCheckinRequest(DateOnly.FromDateTime(DateTime.UtcNow), 5);

        // Act: Second user tries to create checkin for first user's habit
        var response = await client2.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCheckin_DuplicateDate_Returns409()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);
        var localDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var request = new CreateCheckinRequest(localDate, 5);

        // Act: Create first checkin
        var response1 = await client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", request);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        // Act: Try to create duplicate
        var response2 = await client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    [Fact]
    public async Task CreateCheckin_NotPlannedDay_Returns409()
    {
        // Arrange: Create habit with specific days (e.g., Monday only = bit 1)
        var (client, userId) = await CreateAuthenticatedClientAsync();

        // Find a day that is NOT Monday
        var today = DateTime.UtcNow;
        var targetDate = today;
        while (targetDate.DayOfWeek == DayOfWeek.Monday)
        {
            targetDate = targetDate.AddDays(1);
        }

        // Create habit with Monday only (bit 1 = 2)
        var habitId = await CreateHabitAsync(userId, daysOfWeekMask: 2);

        var request = new CreateCheckinRequest(DateOnly.FromDateTime(targetDate), 5);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateCheckin_NegativeActualValue_Returns400()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        var request = new CreateCheckinRequest(DateOnly.FromDateTime(DateTime.UtcNow), -5);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCheckin_FutureDate_Returns400()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        var request = new CreateCheckinRequest(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            5);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCheckin_MoreThan7DaysBack_Returns400()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        var request = new CreateCheckinRequest(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-8)),
            5);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCheckin_ActualValueExceedsTarget_ClampsValue()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId, targetValue: 10);

        var request = new CreateCheckinRequest(
            DateOnly.FromDateTime(DateTime.UtcNow),
            15); // Exceeds target

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var checkin = await response.Content.ReadFromJsonAsync<CheckinResponse>(_options);
        Assert.NotNull(checkin);
        Assert.Equal(10, checkin.ActualValue); // Clamped to target
        Assert.Equal(10, checkin.TargetValueSnapshot);
    }

    [Fact]
    public async Task CreateCheckin_BinaryMode_AcceptsZeroAndOne()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(
            userId: userId,
            completionMode: CompletionMode.Binary,
            targetValue: 1);

        var request = new CreateCheckinRequest(DateOnly.FromDateTime(DateTime.UtcNow), 1);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var checkin = await response.Content.ReadFromJsonAsync<CheckinResponse>(_options);
        Assert.NotNull(checkin);
        Assert.Equal(1, checkin.ActualValue);
        Assert.Equal(CompletionMode.Binary, checkin.CompletionModeSnapshot);
    }

    [Fact]
    public async Task CreateCheckin_StopHabit_CreatesCheckinWithCorrectType()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(
            userId: userId,
            habitType: HabitType.Stop,
            targetValue: 0);

        var request = new CreateCheckinRequest(DateOnly.FromDateTime(DateTime.UtcNow), 0);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var checkin = await response.Content.ReadFromJsonAsync<CheckinResponse>(_options);
        Assert.NotNull(checkin);
        Assert.Equal(HabitType.Stop, checkin.HabitTypeSnapshot);
    }

    [Fact]
    public async Task CreateCheckin_Backfill7Days_AllSucceed()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        // Act: Create checkins for 7 days back
        for (int i = 0; i <= 7; i++)
        {
            var request = new CreateCheckinRequest(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)),
                i + 1);

            var response = await client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }

    // Helper methods

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedClientAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"checkin-test-{Guid.NewGuid()}@test.com";
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

        var client = _fixture.CreateClient();

        var loginRequest = new LoginRequest(testEmail, testPassword);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        foreach (var cookie in cookies)
        {
            client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        return (client, user.Id);
    }

    private async Task<int> CreateHabitAsync(
        Guid userId,
        byte? daysOfWeekMask = null,
        HabitType? habitType = null,
        CompletionMode? completionMode = null,
        short? targetValue = null)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HabitFlow.Data.HabitFlowDbContext>();

        var habit = new Habit
        {
            UserId = userId,
            Title = $"Test Habit {Guid.NewGuid()}",
            Type = habitType ?? HabitType.Start,
            CompletionMode = completionMode ?? CompletionMode.Quantitative,
            DaysOfWeekMask = daysOfWeekMask ?? 127, // All days by default
            TargetValue = targetValue ?? 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Habits.Add(habit);
        await dbContext.SaveChangesAsync();

        return habit.Id;
    }
}
