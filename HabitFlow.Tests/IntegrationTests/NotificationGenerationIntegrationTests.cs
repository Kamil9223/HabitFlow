using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HabitFlow.Api.Contracts.Auth;
using HabitFlow.Api.Contracts.Common;
using HabitFlow.Api.Contracts.Notifications;
using HabitFlow.Core.Abstractions.Notifications;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using HabitFlow.Tests.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HabitFlow.Tests.IntegrationTests;

public class NotificationGenerationIntegrationTests
{
    [Fact]
    public async Task GenerateNotifications_MissDue_CreatesNotification()
    {
        await TestDatabase.EnsureStartedAsync();

        var connectionString = $"{TestDatabase.ConnectionString};Database=HabitFlowTest_{Guid.NewGuid():N}";
        await MigrateAsync(connectionString);

        using var factory = new IntegrationTestFactory(connectionString);
        using var scope = factory.Services.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<HabitFlowDbContext>();

        var email = $"miss-due-{Guid.NewGuid():N}@test.com";
        var password = "Test123!";
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        };

        var createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded);

        var localYesterday = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        var dayMask = GetDayMask(localYesterday);

        var habit = new Habit
        {
            UserId = user.Id,
            Title = "Poranny bieg",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = dayMask,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
        };
        dbContext.Habits.Add(habit);
        await dbContext.SaveChangesAsync();

        var generator = scope.ServiceProvider.GetRequiredService<INotificationGenerationService>();
        var summary = await generator.GenerateNotificationsAsync(CancellationToken.None);
        Assert.Equal(1, summary.NotificationsCreated);

        var repeatSummary = await generator.GenerateNotificationsAsync(CancellationToken.None);
        Assert.Equal(0, repeatSummary.NotificationsCreated);
        Assert.Equal(1, await dbContext.Notifications.CountAsync());

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginRequest = new LoginRequest(email, password);
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        foreach (var cookie in cookies)
        {
            client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }

        var response = await client.GetAsync("/api/v1/notifications");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<NotificationResponse>>(options);
        Assert.NotNull(result);

        var notification = Assert.Single(result.Items);
        Assert.Equal((int)AiGenerationStatus.Fallback, notification.AiStatus);
        Assert.Contains("Poranny bieg", notification.Content);
    }

    private static async Task MigrateAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var context = new HabitFlowDbContext(options);
        await context.Database.MigrateAsync();
    }

    private static byte GetDayMask(DateOnly date)
    {
        var bitIndex = date.DayOfWeek == DayOfWeek.Sunday
            ? 6
            : (int)date.DayOfWeek - 1;

        return (byte)(1 << bitIndex);
    }
}
