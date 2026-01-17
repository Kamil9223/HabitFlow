using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Features.Progress;
using HabitFlow.Core.Services;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class GetProgressRollingQueryHandlerTests
{
    private static HabitFlowDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HabitFlowDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsProgressRollingResult()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var habitId = 1;

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var habit = new Habit
        {
            Id = habitId,
            UserId = userId,
            Title = "Read",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127, // All days
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        // Add check-ins
        var baseDate = new DateOnly(2025, 12, 7);
        context.Checkins.AddRange(
            new Checkin
            {
                HabitId = habitId,
                UserId = userId,
                LocalDate = baseDate.AddDays(-6),
                ActualValue = 10,
                TargetValueSnapshot = 10,
                CompletionModeSnapshot = CompletionMode.Quantitative,
                HabitTypeSnapshot = HabitType.Start,
                IsPlanned = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Checkin
            {
                HabitId = habitId,
                UserId = userId,
                LocalDate = baseDate.AddDays(-3),
                ActualValue = 5,
                TargetValueSnapshot = 10,
                CompletionModeSnapshot = CompletionMode.Quantitative,
                HabitTypeSnapshot = HabitType.Start,
                IsPlanned = true,
                CreatedAtUtc = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetProgressRollingQueryHandler>>();
        var handler = new GetProgressRollingQueryHandler(context, loggedUserContext, logger);
        var query = new GetProgressRollingQuery(habitId, 7, baseDate);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(habitId, result.Value.HabitId);
        Assert.Equal(7, result.Value.WindowDays);
        Assert.Equal(baseDate, result.Value.Until);
        Assert.Equal(7, result.Value.Points.Count); // One point per day in range
    }

    [Fact]
    public async Task Handle_CalculatesCorrectPlannedDaysWithDaysOfWeekMask()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var habitId = 1;

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        // Only Monday and Friday (bit 0 and 4) = 1 + 16 = 17
        var habit = new Habit
        {
            Id = habitId,
            UserId = userId,
            Title = "Read",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 17, // Monday and Friday
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetProgressRollingQueryHandler>>();
        var handler = new GetProgressRollingQueryHandler(context, loggedUserContext, logger);

        // Dec 9, 2025 is Monday - with 7-day window, should have 2 planned days (2 Mondays or 1 Mon + 1 Fri)
        var query = new GetProgressRollingQuery(habitId, 7, new DateOnly(2025, 12, 9));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var lastPoint = result.Value.Points.Last();
        Assert.True(lastPoint.PlannedDays > 0); // Should have at least 1 planned day
    }

    [Fact]
    public async Task Handle_CalculatesCorrectSumDailyScore()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var habitId = 1;

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var habit = new Habit
        {
            Id = habitId,
            UserId = userId,
            Title = "Read",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var baseDate = new DateOnly(2025, 12, 7);

        // Add 2 check-ins with known scores
        context.Checkins.AddRange(
            new Checkin
            {
                HabitId = habitId,
                UserId = userId,
                LocalDate = baseDate.AddDays(-1),
                ActualValue = 10, // Score = 1.0 (10/10)
                TargetValueSnapshot = 10,
                CompletionModeSnapshot = CompletionMode.Quantitative,
                HabitTypeSnapshot = HabitType.Start,
                IsPlanned = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Checkin
            {
                HabitId = habitId,
                UserId = userId,
                LocalDate = baseDate,
                ActualValue = 5, // Score = 0.5 (5/10)
                TargetValueSnapshot = 10,
                CompletionModeSnapshot = CompletionMode.Quantitative,
                HabitTypeSnapshot = HabitType.Start,
                IsPlanned = true,
                CreatedAtUtc = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetProgressRollingQueryHandler>>();
        var handler = new GetProgressRollingQueryHandler(context, loggedUserContext, logger);
        var query = new GetProgressRollingQuery(habitId, 7, baseDate);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var lastPoint = result.Value.Points.Last();
        Assert.Equal(1.5, lastPoint.SumDailyScore, precision: 2); // 1.0 + 0.5 = 1.5
    }

    [Fact]
    public async Task Handle_CalculatesCorrectSuccessRate()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var habitId = 1;

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var habit = new Habit
        {
            Id = habitId,
            UserId = userId,
            Title = "Read",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127, // All 7 days
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var baseDate = new DateOnly(2025, 12, 7);

        // Add check-in with score 0.5
        context.Checkins.Add(new Checkin
        {
            HabitId = habitId,
            UserId = userId,
            LocalDate = baseDate,
            ActualValue = 5, // Score = 0.5
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetProgressRollingQueryHandler>>();
        var handler = new GetProgressRollingQueryHandler(context, loggedUserContext, logger);
        var query = new GetProgressRollingQuery(habitId, 7, baseDate);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var lastPoint = result.Value.Points.Last();
        // 7 planned days, sum = 0.5, rate = 0.5/7 ≈ 0.071
        Assert.True(lastPoint.SuccessRate > 0);
        Assert.True(lastPoint.SuccessRate < 0.1);
    }

    [Fact]
    public async Task Handle_ReturnsZeroSuccessRateWhenNoPlannedDays()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var habitId = 1;

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var habit = new Habit
        {
            Id = habitId,
            UserId = userId,
            Title = "Read",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 0, // No days planned
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetProgressRollingQueryHandler>>();
        var handler = new GetProgressRollingQueryHandler(context, loggedUserContext, logger);
        var query = new GetProgressRollingQuery(habitId, 7, new DateOnly(2025, 12, 7));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.All(result.Value.Points, point =>
        {
            Assert.Equal(0, point.PlannedDays);
            Assert.Equal(0.0, point.SuccessRate);
        });
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundWhenHabitDoesNotExist()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var nonExistentHabitId = 999;

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetProgressRollingQueryHandler>>();
        var handler = new GetProgressRollingQueryHandler(context, loggedUserContext, logger);
        var query = new GetProgressRollingQuery(nonExistentHabitId, 7, new DateOnly(2025, 12, 7));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Habit.NotFound", result.Error.Code);
        Assert.Contains("not found", result.Error.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundWhenHabitBelongsToAnotherUser()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        context.Users.AddRange(
            new ApplicationUser
            {
                Id = userId1,
                TimeZoneId = "UTC",
                CreatedAtUtc = DateTime.UtcNow
            },
            new ApplicationUser
            {
                Id = userId2,
                TimeZoneId = "UTC",
                CreatedAtUtc = DateTime.UtcNow
            }
        );

        var habit = new Habit
        {
            UserId = userId2,
            Title = "User2's Habit",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        // User1 tries to access user2's habit
        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId1, "UTC", "user1@example.com"));

        var logger = Substitute.For<ILogger<GetProgressRollingQueryHandler>>();
        var handler = new GetProgressRollingQueryHandler(context, loggedUserContext, logger);
        var query = new GetProgressRollingQuery(habit.Id, 7, new DateOnly(2025, 12, 7));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Habit.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_UsesTodayWhenUntilIsNull()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var habitId = 1;

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var habit = new Habit
        {
            Id = habitId,
            UserId = userId,
            Title = "Read",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetProgressRollingQueryHandler>>();
        var handler = new GetProgressRollingQueryHandler(context, loggedUserContext, logger);
        var query = new GetProgressRollingQuery(habitId, 7, null); // Until is null

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.True(result.Value.Until >= todayUtc.AddDays(-1)); // Allow for date boundary
        Assert.True(result.Value.Until <= todayUtc.AddDays(1));
    }

    [Fact]
    public async Task Handle_WorksWith30DayWindow()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var habitId = 1;

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var habit = new Habit
        {
            Id = habitId,
            UserId = userId,
            Title = "Read",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetProgressRollingQueryHandler>>();
        var handler = new GetProgressRollingQueryHandler(context, loggedUserContext, logger);
        var query = new GetProgressRollingQuery(habitId, 30, new DateOnly(2025, 12, 31));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Value.WindowDays);
        Assert.Equal(30, result.Value.Points.Count); // 30 points for 30-day range
    }

    [Fact]
    public async Task Handle_ReturnsValidationErrorWhenHabitIdIsZero()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetProgressRollingQueryHandler>>();
        var handler = new GetProgressRollingQueryHandler(context, loggedUserContext, logger);
        var query = new GetProgressRollingQuery(0, 7, new DateOnly(2025, 12, 7));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "HabitId");
    }

    [Fact]
    public async Task Handle_ReturnsValidationErrorWhenWindowDaysInvalid()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetProgressRollingQueryHandler>>();
        var handler = new GetProgressRollingQueryHandler(context, loggedUserContext, logger);
        var query = new GetProgressRollingQuery(1, 15, new DateOnly(2025, 12, 7));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "WindowDays");
    }
}
