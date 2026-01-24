using HabitFlow.Core.Abstractions.Notifications;
using HabitFlow.Core.Options;
using HabitFlow.Core.Services.Notifications;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using HabitFlow.Core.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class NotificationGenerationServiceTests
{
    [Fact]
    public async Task GenerateNotificationsAsync_MissDueHabit_CreatesNotification()
    {
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var localYesterday = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        var dayMask = GetDayMask(localYesterday);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        });

        var habit = new Habit
        {
            UserId = userId,
            Title = "Bieganie",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = dayMask,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var generator = Substitute.For<INotificationContentGenerator>();
        generator.GenerateAsync(Arg.Any<NotificationContentContext>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationContentResult("Testowa notyfikacja dzis", AiGenerationStatus.Fallback, "AI niedostepne"));

        var service = CreateService(context, generator);

        var summary = await service.GenerateNotificationsAsync(CancellationToken.None);

        Assert.Equal(1, summary.NotificationsCreated);
        Assert.Equal(0, summary.Errors);
        var notification = await context.Notifications.SingleAsync();
        Assert.Equal(habit.Id, notification.HabitId);
        Assert.Equal(localYesterday, notification.LocalDate);
        Assert.Equal("Testowa notyfikacja dzis", notification.Content);
    }

    [Fact]
    public async Task GenerateNotificationsAsync_CheckinExists_SkipsNotification()
    {
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var localYesterday = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        var dayMask = GetDayMask(localYesterday);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        });

        var habit = new Habit
        {
            UserId = userId,
            Title = "Czytanie",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = dayMask,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        context.Checkins.Add(new Checkin
        {
            HabitId = habit.Id,
            UserId = userId,
            LocalDate = localYesterday,
            ActualValue = 1,
            TargetValueSnapshot = 1,
            CompletionModeSnapshot = CompletionMode.Binary,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var generator = Substitute.For<INotificationContentGenerator>();
        var service = CreateService(context, generator);

        var summary = await service.GenerateNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, summary.NotificationsCreated);
        Assert.Empty(context.Notifications);
    }

    [Fact]
    public async Task GenerateNotificationsAsync_ExistingNotification_SkipsDuplicate()
    {
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var localYesterday = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        var dayMask = GetDayMask(localYesterday);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        });

        var habit = new Habit
        {
            UserId = userId,
            Title = "Medytacja",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = dayMask,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        context.Notifications.Add(new Notification
        {
            UserId = userId,
            HabitId = habit.Id,
            LocalDate = localYesterday,
            Type = NotificationType.MissDue,
            Content = "Juz istnieje",
            AiStatus = AiGenerationStatus.Fallback,
            CreatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var generator = Substitute.For<INotificationContentGenerator>();
        var service = CreateService(context, generator);

        var summary = await service.GenerateNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, summary.NotificationsCreated);
        Assert.Equal(1, await context.Notifications.CountAsync());
    }

    [Fact]
    public async Task GenerateNotificationsAsync_DeadlinePassed_SkipsNotification()
    {
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var localYesterday = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        var dayMask = GetDayMask(localYesterday);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        });

        var habit = new Habit
        {
            UserId = userId,
            Title = "Pisanie",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = dayMask,
            TargetValue = 1,
            DeadlineDate = localYesterday.AddDays(-1),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var generator = Substitute.For<INotificationContentGenerator>();
        var service = CreateService(context, generator);

        var summary = await service.GenerateNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, summary.NotificationsCreated);
        Assert.Empty(context.Notifications);
    }

    [Fact]
    public async Task GenerateNotificationsAsync_InvalidTimeZone_IncrementsErrors()
    {
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "Invalid/Zone",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        });
        await context.SaveChangesAsync();

        var generator = Substitute.For<INotificationContentGenerator>();
        var service = CreateService(context, generator);

        var summary = await service.GenerateNotificationsAsync(CancellationToken.None);

        Assert.Equal(1, summary.Errors);
        Assert.Equal(0, summary.NotificationsCreated);
    }

    [Fact]
    public async Task GenerateNotificationsAsync_BlockedContent_UsesSafeFallback()
    {
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var localYesterday = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        var dayMask = GetDayMask(localYesterday);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        });

        var habit = new Habit
        {
            UserId = userId,
            Title = "Nawyk",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = dayMask,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var generator = Substitute.For<INotificationContentGenerator>();
        generator.GenerateAsync(Arg.Any<NotificationContentContext>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationContentResult("zabij", AiGenerationStatus.Success, null));

        var service = CreateService(context, generator);

        var summary = await service.GenerateNotificationsAsync(CancellationToken.None);

        Assert.Equal(1, summary.NotificationsCreated);
        var notification = await context.Notifications.SingleAsync();
        Assert.Equal(AiGenerationStatus.Error, notification.AiStatus);
        Assert.Equal("Wczoraj nie udalo sie zrobic nawyku. Wrocmy na dobre tory!", notification.Content);
        Assert.False(string.IsNullOrWhiteSpace(notification.AiError));
    }

    [Fact]
    public async Task GenerateNotificationsAsync_AiBudgetZero_UsesFallback()
    {
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var localYesterday = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        var dayMask = GetDayMask(localYesterday);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
        });

        var habit = new Habit
        {
            UserId = userId,
            Title = "Budzet AI",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = dayMask,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var generator = Substitute.For<INotificationContentGenerator>();
        generator.GenerateAsync(Arg.Any<NotificationContentContext>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationContentResult("AI tresc testowa dzis", AiGenerationStatus.Success, null));

        var service = CreateService(context, generator, maxDailyRequests: 0);

        var summary = await service.GenerateNotificationsAsync(CancellationToken.None);

        Assert.Equal(1, summary.NotificationsCreated);
        var notification = await context.Notifications.SingleAsync();
        Assert.Equal(AiGenerationStatus.Fallback, notification.AiStatus);
    }

    private static HabitFlowDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HabitFlowDbContext(options);
    }

    private static NotificationGenerationService CreateService(
        HabitFlowDbContext context,
        INotificationContentGenerator generator,
        int maxDailyRequests = 10)
    {
        var repository = new NotificationRepository(context);
        var jobOptions = Options.Create(new NotificationJobSettings
        {
            BatchSize = 50
        });
        var llmOptions = Options.Create(new LlmSettings
        {
            Enabled = true,
            MaxDailyRequests = maxDailyRequests
        });
        var features = Options.Create(new NotificationFeaturesOptions
        {
            NotificationsEnabled = true,
            AiNotifications = new NotificationFeaturesOptions.AiNotificationsOptions
            {
                Enabled = true,
                FallbackOnly = false
            }
        });

        return new NotificationGenerationService(
            context,
            repository,
            generator,
            jobOptions,
            features,
            llmOptions,
            new FallbackContentGenerator(new Random(4)),
            NullLogger<NotificationGenerationService>.Instance);
    }

    private static byte GetDayMask(DateOnly date)
    {
        var bitIndex = date.DayOfWeek == DayOfWeek.Sunday
            ? 6
            : (int)date.DayOfWeek - 1;

        return (byte)(1 << bitIndex);
    }
}
