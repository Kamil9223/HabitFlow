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

    [Fact]
    public async Task GetCheckinsByDate_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _fixture.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await client.GetAsync($"/api/v1/checkins?date={date:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCheckinsByDate_ValidRequest_Returns200WithCheckins()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId1 = await CreateHabitAsync(userId);
        var habitId2 = await CreateHabitAsync(userId);
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Create checkins for target date
        var request1 = new CreateCheckinRequest(targetDate, 7);
        var response1 = await client.PostAsJsonAsync($"/api/v1/habits/{habitId1}/checkins", request1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        var request2 = new CreateCheckinRequest(targetDate, 5);
        var response2 = await client.PostAsJsonAsync($"/api/v1/habits/{habitId2}/checkins", request2);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        // Act
        var response = await client.GetAsync($"/api/v1/checkins?date={targetDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CheckinsByDateResponse>(_options);
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Equal(targetDate, item.LocalDate));
    }

    [Fact]
    public async Task GetCheckinsByDate_NoCheckins_ReturnsEmptyList()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        await CreateHabitAsync(userId); // Create habit but no checkins
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await client.GetAsync($"/api/v1/checkins?date={targetDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CheckinsByDateResponse>(_options);
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetCheckinsByDate_OnlyReturnsCurrentUserCheckins()
    {
        // Arrange: Create two users with habits and checkins
        var (client1, userId1) = await CreateAuthenticatedClientAsync();
        var habitId1 = await CreateHabitAsync(userId1);
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var (client2, userId2) = await CreateAuthenticatedClientAsync();
        var habitId2 = await CreateHabitAsync(userId2);

        // Create checkins for both users on same date
        var request1 = new CreateCheckinRequest(targetDate, 7);
        var response1 = await client1.PostAsJsonAsync($"/api/v1/habits/{habitId1}/checkins", request1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        var request2 = new CreateCheckinRequest(targetDate, 5);
        var response2 = await client2.PostAsJsonAsync($"/api/v1/habits/{habitId2}/checkins", request2);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        // Act: Get checkins for user1
        var response = await client1.GetAsync($"/api/v1/checkins?date={targetDate:yyyy-MM-dd}");

        // Assert: Should only return user1's checkin
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CheckinsByDateResponse>(_options);
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(habitId1, result.Items[0].HabitId);
    }

    [Fact]
    public async Task GetCheckinsByDate_InvalidDateFormat_Returns400()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/checkins?date=invalid-date");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCheckinsByDate_MissingDateParameter_Returns400()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/checkins");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCheckinsByDate_OnlyReturnsCheckinsForSpecifiedDate()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = targetDate.AddDays(-1);

        // Create checkins for two different dates
        var requestToday = new CreateCheckinRequest(targetDate, 7);
        var responseToday = await client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", requestToday);
        Assert.Equal(HttpStatusCode.Created, responseToday.StatusCode);

        var requestYesterday = new CreateCheckinRequest(yesterday, 5);
        var responseYesterday = await client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", requestYesterday);
        Assert.Equal(HttpStatusCode.Created, responseYesterday.StatusCode);

        // Act: Get checkins for target date only
        var response = await client.GetAsync($"/api/v1/checkins?date={targetDate:yyyy-MM-dd}");

        // Assert: Should only return today's checkin
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CheckinsByDateResponse>(_options);
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(targetDate, result.Items[0].LocalDate);
        Assert.Equal(7, result.Items[0].ActualValue);
    }

    [Fact]
    public async Task GetCheckinsByDate_ReturnsAllRequiredFields()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var request = new CreateCheckinRequest(targetDate, 7);
        var createResponse = await client.PostAsJsonAsync($"/api/v1/habits/{habitId}/checkins", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdCheckin = await createResponse.Content.ReadFromJsonAsync<CheckinResponse>(_options);
        Assert.NotNull(createdCheckin);

        // Act
        var response = await client.GetAsync($"/api/v1/checkins?date={targetDate:yyyy-MM-dd}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CheckinsByDateResponse>(_options);
        Assert.NotNull(result);
        var item = Assert.Single(result.Items);
        Assert.Equal(createdCheckin.Id, item.Id);
        Assert.Equal(habitId, item.HabitId);
        Assert.Equal(targetDate, item.LocalDate);
        Assert.Equal(7, item.ActualValue);
        Assert.True(item.IsPlanned);
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
