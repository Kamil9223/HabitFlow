using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HabitFlow.Api.Contracts.Progress;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using HabitFlow.Tests.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HabitFlow.Tests.IntegrationTests;

public class ProgressEndpointsTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly JsonSerializerOptions _options;

    public ProgressEndpointsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _options.Converters.Add(new JsonStringEnumConverter());
    }

    [Fact]
    public async Task GetProgressRolling_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/habits/1/progress/rolling?windowDays=7");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProgressRolling_ValidRequest_Returns200OK()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        // Act
        var response = await client.GetAsync($"/api/v1/habits/{habitId}/progress/rolling?windowDays=7");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProgressRollingResponse>(_options);
        Assert.NotNull(result);
        Assert.Equal(habitId, result.HabitId);
        Assert.Equal(7, result.WindowDays);
        Assert.NotNull(result.Points);
        Assert.Equal(7, result.Points.Count); // 7 days in result
    }

    [Fact]
    public async Task GetProgressRolling_With30DayWindow_Returns200OK()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        // Act
        var response = await client.GetAsync($"/api/v1/habits/{habitId}/progress/rolling?windowDays=30");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProgressRollingResponse>(_options);
        Assert.NotNull(result);
        Assert.Equal(30, result.WindowDays);
        Assert.Equal(30, result.Points.Count);
    }

    [Fact]
    public async Task GetProgressRolling_HabitNotFound_Returns404()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/habits/999999/progress/rolling?windowDays=7");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProgressRolling_HabitBelongsToOtherUser_Returns404()
    {
        // Arrange: Create habit for user1
        var (client1, userId1) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId1);

        // Create user2 and try to access user1's habit
        var (client2, _) = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client2.GetAsync($"/api/v1/habits/{habitId}/progress/rolling?windowDays=7");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProgressRolling_InvalidWindowDays_Returns400()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        // Act
        var response = await client.GetAsync($"/api/v1/habits/{habitId}/progress/rolling?windowDays=15");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProgressRolling_ZeroWindowDays_Returns400()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        // Act
        var response = await client.GetAsync($"/api/v1/habits/{habitId}/progress/rolling?windowDays=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProgressRolling_WithUntilDate_Returns200OK()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);
        var until = "2025-12-07";

        // Act
        var response = await client.GetAsync($"/api/v1/habits/{habitId}/progress/rolling?windowDays=7&until={until}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProgressRollingResponse>(_options);
        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2025, 12, 7), result.Until);
    }

    [Fact]
    public async Task GetProgressRolling_WithCheckins_CalculatesCorrectMetrics()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        // Create some check-ins
        await CreateCheckinAsync(habitId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), 10);
        await CreateCheckinAsync(habitId, DateOnly.FromDateTime(DateTime.UtcNow), 5);

        // Act
        var response = await client.GetAsync($"/api/v1/habits/{habitId}/progress/rolling?windowDays=7");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProgressRollingResponse>(_options);
        Assert.NotNull(result);
        Assert.All(result.Points, point =>
        {
            Assert.True(point.PlannedDays >= 0);
            Assert.True(point.SumDailyScore >= 0);
            Assert.True(point.SuccessRate >= 0);
        });
    }

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedClientAsync()
    {
        var userId = Guid.NewGuid();
        var email = $"testuser{Guid.NewGuid()}@example.com";
        var password = "Test123!";

        using var scope = _fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = email,
            Email = email,
            TimeZoneId = "UTC",
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var client = _fixture.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = email,
            password = password
        });

        loginResponse.EnsureSuccessStatusCode();

        return (client, userId);
    }

    private async Task<int> CreateHabitAsync(Guid userId, byte daysOfWeekMask = 127)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HabitFlow.Data.HabitFlowDbContext>();

        var habit = new Habit
        {
            UserId = userId,
            Title = $"Test Habit {Guid.NewGuid()}",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = daysOfWeekMask,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        return habit.Id;
    }

    private async Task CreateCheckinAsync(int habitId, DateOnly localDate, int actualValue)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HabitFlow.Data.HabitFlowDbContext>();

        var habit = await context.Habits.FindAsync(habitId);
        if (habit == null) throw new Exception("Habit not found");

        var checkin = new Checkin
        {
            HabitId = habitId,
            UserId = habit.UserId,
            LocalDate = localDate,
            ActualValue = actualValue,
            TargetValueSnapshot = habit.TargetValue,
            CompletionModeSnapshot = habit.CompletionMode,
            HabitTypeSnapshot = habit.Type,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Checkins.Add(checkin);
        await context.SaveChangesAsync();
    }
}
