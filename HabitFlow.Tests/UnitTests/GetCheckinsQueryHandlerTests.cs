using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Features.Checkins;
using HabitFlow.Core.Services;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class GetCheckinsQueryHandlerTests
{
    private static HabitFlowDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HabitFlowDbContext(options);
    }

    [Fact]
    public async Task Handle_ReturnsCheckinsForHabitInDateRange()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var from = new DateOnly(2025, 11, 1);
        var to = new DateOnly(2025, 11, 30);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var habit = new Habit
        {
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

        // Add checkins within range
        var checkin1 = new Checkin
        {
            HabitId = habit.Id,
            UserId = userId,
            LocalDate = new DateOnly(2025, 11, 2),
            ActualValue = 7,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var checkin2 = new Checkin
        {
            HabitId = habit.Id,
            UserId = userId,
            LocalDate = new DateOnly(2025, 11, 15),
            ActualValue = 8,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Checkin outside range - should not be returned
        var checkinOutsideRange = new Checkin
        {
            HabitId = habit.Id,
            UserId = userId,
            LocalDate = new DateOnly(2025, 12, 1),
            ActualValue = 5,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Checkins.AddRange(checkin1, checkin2, checkinOutsideRange);
        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetCheckinsQueryHandler>>();
        var handler = new GetCheckinsQueryHandler(context, loggedUserContext, logger);
        var query = new GetCheckinsQuery(habit.Id, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(habit.Id, result.Value.HabitId);
        Assert.Equal(from, result.Value.From);
        Assert.Equal(to, result.Value.To);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.All(result.Value.Items, item =>
        {
            Assert.True(item.LocalDate >= from && item.LocalDate <= to);
        });
    }

    [Fact]
    public async Task Handle_ReturnsEmptyListWhenNoCheckinsInRange()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var from = new DateOnly(2025, 11, 1);
        var to = new DateOnly(2025, 11, 30);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var habit = new Habit
        {
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

        var logger = Substitute.For<ILogger<GetCheckinsQueryHandler>>();
        var handler = new GetCheckinsQueryHandler(context, loggedUserContext, logger);
        var query = new GetCheckinsQuery(habit.Id, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundWhenHabitDoesNotExist()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var nonExistentHabitId = 999;
        var from = new DateOnly(2025, 11, 1);
        var to = new DateOnly(2025, 11, 30);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetCheckinsQueryHandler>>();
        var handler = new GetCheckinsQueryHandler(context, loggedUserContext, logger);
        var query = new GetCheckinsQuery(nonExistentHabitId, from, to);

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
        var from = new DateOnly(2025, 11, 1);
        var to = new DateOnly(2025, 11, 30);

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

        // Habit belongs to user2
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

        var logger = Substitute.For<ILogger<GetCheckinsQueryHandler>>();
        var handler = new GetCheckinsQueryHandler(context, loggedUserContext, logger);
        var query = new GetCheckinsQuery(habit.Id, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - should return NotFound (not Forbidden to avoid resource enumeration)
        Assert.False(result.IsSuccess);
        Assert.Equal("Habit.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_SortsByLocalDateAscending()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var from = new DateOnly(2025, 11, 1);
        var to = new DateOnly(2025, 11, 30);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var habit = new Habit
        {
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

        // Add checkins in non-sorted order
        var date1 = new DateOnly(2025, 11, 15);
        var date2 = new DateOnly(2025, 11, 5);
        var date3 = new DateOnly(2025, 11, 25);

        context.Checkins.AddRange(
            new Checkin
            {
                HabitId = habit.Id,
                UserId = userId,
                LocalDate = date1,
                ActualValue = 1,
                TargetValueSnapshot = 1,
                CompletionModeSnapshot = CompletionMode.Binary,
                HabitTypeSnapshot = HabitType.Start,
                IsPlanned = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Checkin
            {
                HabitId = habit.Id,
                UserId = userId,
                LocalDate = date2,
                ActualValue = 1,
                TargetValueSnapshot = 1,
                CompletionModeSnapshot = CompletionMode.Binary,
                HabitTypeSnapshot = HabitType.Start,
                IsPlanned = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Checkin
            {
                HabitId = habit.Id,
                UserId = userId,
                LocalDate = date3,
                ActualValue = 1,
                TargetValueSnapshot = 1,
                CompletionModeSnapshot = CompletionMode.Binary,
                HabitTypeSnapshot = HabitType.Start,
                IsPlanned = true,
                CreatedAtUtc = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetCheckinsQueryHandler>>();
        var handler = new GetCheckinsQueryHandler(context, loggedUserContext, logger);
        var query = new GetCheckinsQuery(habit.Id, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Items.Count);
        Assert.Equal(date2, result.Value.Items[0].LocalDate);
        Assert.Equal(date1, result.Value.Items[1].LocalDate);
        Assert.Equal(date3, result.Value.Items[2].LocalDate);
    }

    [Fact]
    public async Task Handle_ReturnsAllRequiredFieldsWithSnapshots()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var from = new DateOnly(2025, 11, 1);
        var to = new DateOnly(2025, 11, 30);
        var targetDate = new DateOnly(2025, 11, 10);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var habit = new Habit
        {
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

        var checkin = new Checkin
        {
            HabitId = habit.Id,
            UserId = userId,
            LocalDate = targetDate,
            ActualValue = 7,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Checkins.Add(checkin);
        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetCheckinsQueryHandler>>();
        var handler = new GetCheckinsQueryHandler(context, loggedUserContext, logger);
        var query = new GetCheckinsQuery(habit.Id, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(checkin.Id, item.Id);
        Assert.Equal(targetDate, item.LocalDate);
        Assert.Equal(7, item.ActualValue);
        Assert.Equal(10, item.TargetValueSnapshot);
        Assert.Equal((byte)CompletionMode.Quantitative, item.CompletionModeSnapshot);
        Assert.Equal((byte)HabitType.Start, item.HabitTypeSnapshot);
        Assert.True(item.IsPlanned);
    }

    [Fact]
    public async Task Handle_ReturnsValidationErrorWhenHabitIdIsZero()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var from = new DateOnly(2025, 11, 1);
        var to = new DateOnly(2025, 11, 30);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var logger = Substitute.For<ILogger<GetCheckinsQueryHandler>>();
        var handler = new GetCheckinsQueryHandler(context, loggedUserContext, logger);
        var query = new GetCheckinsQuery(0, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "HabitId");
    }

    [Fact]
    public async Task Handle_ReturnsValidationErrorWhenFromIsAfterTo()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var from = new DateOnly(2025, 11, 30);
        var to = new DateOnly(2025, 11, 1);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var habit = new Habit
        {
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

        var logger = Substitute.For<ILogger<GetCheckinsQueryHandler>>();
        var handler = new GetCheckinsQueryHandler(context, loggedUserContext, logger);
        var query = new GetCheckinsQuery(habit.Id, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "From");
    }

    [Fact]
    public async Task Handle_ReturnsValidationErrorWhenDateRangeExceeds365Days()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var from = new DateOnly(2025, 1, 1);
        var to = new DateOnly(2026, 1, 2); // 367 days

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var habit = new Habit
        {
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

        var logger = Substitute.For<ILogger<GetCheckinsQueryHandler>>();
        var handler = new GetCheckinsQueryHandler(context, loggedUserContext, logger);
        var query = new GetCheckinsQuery(habit.Id, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "DateRange");
    }
}
