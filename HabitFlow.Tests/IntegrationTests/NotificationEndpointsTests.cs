using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HabitFlow.Api.Contracts.Auth;
using HabitFlow.Api.Contracts.Common;
using HabitFlow.Api.Contracts.Notifications;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using HabitFlow.Tests.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HabitFlow.Tests.IntegrationTests;

public class NotificationEndpointsTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly JsonSerializerOptions _options;

    public NotificationEndpointsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _options.Converters.Add(new JsonStringEnumConverter());
    }

    [Fact]
    public async Task GetNotifications_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/notifications");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetNotifications_ValidRequest_Returns200WithPagedNotifications()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        // Create test notifications
        await CreateNotificationAsync(userId, habitId, DateOnly.FromDateTime(DateTime.UtcNow), "Notification 1");
        await CreateNotificationAsync(userId, habitId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), "Notification 2");

        // Act
        var response = await client.GetAsync("/api/v1/notifications");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(_options);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetNotifications_EmptyList_Returns200WithEmptyResult()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/notifications");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(_options);
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetNotifications_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        // Create 25 notifications
        for (int i = 0; i < 25; i++)
        {
            await CreateNotificationAsync(userId, habitId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)), $"Notification {i}");
        }

        // Act: Get page 2 with 10 items
        var response = await client.GetAsync("/api/v1/notifications?page=2&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(_options);
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
    }

    [Fact]
    public async Task GetNotifications_PageSizeExceedsMax_ClampsTo100()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        // Create 150 notifications (with different dates to avoid unique constraint)
        for (int i = 0; i < 150; i++)
        {
            await CreateNotificationAsync(userId, habitId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)), $"Notification {i}");
        }

        // Act: Request 200 items (should be clamped to 100)
        var response = await client.GetAsync("/api/v1/notifications?pageSize=200");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(_options);
        Assert.NotNull(result);
        Assert.Equal(150, result.TotalCount);
        Assert.Equal(100, result.Items.Count); // Clamped to max 100
    }

    [Fact]
    public async Task GetNotifications_SortByCreatedAtUtcDesc_ReturnsSortedNotifications()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        var notification1 = await CreateNotificationAsync(userId, habitId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)), "Oldest", DateTime.UtcNow.AddDays(-10), AiGenerationStatus.Success);
        var notification2 = await CreateNotificationAsync(userId, habitId, DateOnly.FromDateTime(DateTime.UtcNow), "Newest", DateTime.UtcNow, AiGenerationStatus.Success);
        var notification3 = await CreateNotificationAsync(userId, habitId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), "Middle", DateTime.UtcNow.AddDays(-5), AiGenerationStatus.Success);

        // Act
        var response = await client.GetAsync("/api/v1/notifications?sortField=CreatedAtUtc&sortDirection=Desc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(_options);
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal("Newest", result.Items[0].Content);
        Assert.Equal("Middle", result.Items[1].Content);
        Assert.Equal("Oldest", result.Items[2].Content);
    }

    [Fact]
    public async Task GetNotifications_SortByLocalDateAsc_ReturnsSortedNotifications()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await CreateNotificationAsync(userId, habitId, today.AddDays(-5), "Oldest date");
        await CreateNotificationAsync(userId, habitId, today, "Newest date");
        await CreateNotificationAsync(userId, habitId, today.AddDays(-2), "Middle date");

        // Act
        var response = await client.GetAsync("/api/v1/notifications?sortField=LocalDate&sortDirection=Asc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(_options);
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal("Oldest date", result.Items[0].Content);
        Assert.Equal("Middle date", result.Items[1].Content);
        Assert.Equal("Newest date", result.Items[2].Content);
    }

    [Fact]
    public async Task GetNotifications_UserIsolation_ReturnsOnlyCurrentUserNotifications()
    {
        // Arrange: Create two users
        var (client1, userId1) = await CreateAuthenticatedClientAsync();
        var habitId1 = await CreateHabitAsync(userId1);

        var (client2, userId2) = await CreateAuthenticatedClientAsync();
        var habitId2 = await CreateHabitAsync(userId2);

        // Create notifications for both users (with different dates to avoid unique constraint)
        await CreateNotificationAsync(userId1, habitId1, DateOnly.FromDateTime(DateTime.UtcNow), "User 1 notification 1");
        await CreateNotificationAsync(userId2, habitId2, DateOnly.FromDateTime(DateTime.UtcNow), "User 2 notification");
        await CreateNotificationAsync(userId1, habitId1, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), "User 1 notification 2");

        // Act: Get notifications for user 1
        var response = await client1.GetAsync("/api/v1/notifications");

        // Assert: Should only return user 1's notifications
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(_options);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, n => Assert.Contains("User 1", n.Content));
        Assert.DoesNotContain(result.Items, n => n.Content.Contains("User 2"));
    }

    [Fact]
    public async Task GetNotifications_PageOutOfRange_ReturnsEmptyList()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        // Create 10 notifications (with different dates to avoid unique constraint)
        for (int i = 0; i < 10; i++)
        {
            await CreateNotificationAsync(userId, habitId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)), $"Notification {i}");
        }

        // Act: Request page 999
        var response = await client.GetAsync("/api/v1/notifications?page=999&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(_options);
        Assert.NotNull(result);
        Assert.Equal(10, result.TotalCount); // Total count should still be 10
        Assert.Empty(result.Items); // But items should be empty
    }

    [Fact]
    public async Task GetNotifications_ReturnsAllRequiredFields()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        var localDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var notificationId = await CreateNotificationAsync(
            userId,
            habitId,
            localDate,
            "Test notification content",
            null,
            AiGenerationStatus.Success);

        // Act
        var response = await client.GetAsync("/api/v1/notifications");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(_options);
        Assert.NotNull(result);
        var notification = Assert.Single(result.Items);

        Assert.Equal(notificationId, notification.Id);
        Assert.Equal(habitId, notification.HabitId);
        Assert.Equal(localDate, notification.LocalDate);
        Assert.Equal((int)NotificationType.MissDue, notification.Type);
        Assert.Equal("Test notification content", notification.Content);
        Assert.Equal((int)AiGenerationStatus.Success, notification.AiStatus);
        Assert.NotEqual(default, notification.CreatedAtUtc);
    }

    [Fact]
    public async Task GetNotifications_NullAiStatus_ReturnsNotificationWithNullAiStatus()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync();
        var habitId = await CreateHabitAsync(userId);

        await CreateNotificationAsync(userId, habitId, DateOnly.FromDateTime(DateTime.UtcNow), "No AI status", null, null);

        // Act
        var response = await client.GetAsync("/api/v1/notifications");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(_options);
        Assert.NotNull(result);
        var notification = Assert.Single(result.Items);
        Assert.Null(notification.AiStatus);
    }

    // Helper methods

    private async Task<(HttpClient client, Guid userId)> CreateAuthenticatedClientAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var testEmail = $"notification-test-{Guid.NewGuid()}@test.com";
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

    private async Task<int> CreateHabitAsync(Guid userId)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HabitFlow.Data.HabitFlowDbContext>();

        var habit = new Habit
        {
            UserId = userId,
            Title = $"Test Habit {Guid.NewGuid()}",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Habits.Add(habit);
        await dbContext.SaveChangesAsync();

        return habit.Id;
    }

    private Task<long> CreateNotificationAsync(
        Guid userId,
        int habitId,
        DateOnly localDate,
        string content) =>
        CreateNotificationAsync(userId, habitId, localDate, content, null, AiGenerationStatus.Success);

    private async Task<long> CreateNotificationAsync(
        Guid userId,
        int habitId,
        DateOnly localDate,
        string content,
        DateTime? createdAtUtc,
        AiGenerationStatus? aiStatus)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HabitFlow.Data.HabitFlowDbContext>();

        var notification = new Notification
        {
            UserId = userId,
            HabitId = habitId,
            LocalDate = localDate,
            Type = NotificationType.MissDue,
            Content = content,
            AiStatus = aiStatus,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();

        return notification.Id;
    }
}
