using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Features.Checkins;
using HabitFlow.Core.Services;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class GetCheckinsByDateQueryHandlerTests
{
    private static HabitFlowDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HabitFlowDbContext(options);
    }

    [Fact]
    public async Task Handle_ReturnsCheckinsForSpecificDate()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var targetDate = new DateOnly(2025, 12, 7);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var habit1 = new Habit
        {
            UserId = userId,
            Title = "Read",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        var habit2 = new Habit
        {
            UserId = userId,
            Title = "Exercise",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.AddRange(habit1, habit2);
        await context.SaveChangesAsync();

        var checkin1 = new Checkin
        {
            HabitId = habit1.Id,
            UserId = userId,
            LocalDate = targetDate,
            ActualValue = 7,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var checkin2 = new Checkin
        {
            HabitId = habit2.Id,
            UserId = userId,
            LocalDate = targetDate,
            ActualValue = 1,
            TargetValueSnapshot = 1,
            CompletionModeSnapshot = CompletionMode.Binary,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Checkin for different date - should not be returned
        var checkinDifferentDate = new Checkin
        {
            HabitId = habit1.Id,
            UserId = userId,
            LocalDate = targetDate.AddDays(-1),
            ActualValue = 5,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Checkins.AddRange(checkin1, checkin2, checkinDifferentDate);
        await context.SaveChangesAsync();

        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId, "UTC", "test@example.com"));

        var handler = new GetCheckinsByDateQueryHandler(context, loggedUserContext);
        var query = new GetCheckinsByDateQuery(targetDate);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, item => Assert.Equal(targetDate, item.LocalDate));
        Assert.Contains(result.Value, c => c.HabitId == habit1.Id && c.ActualValue == 7);
        Assert.Contains(result.Value, c => c.HabitId == habit2.Id && c.ActualValue == 1);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyListWhenNoCheckinsExist()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var targetDate = new DateOnly(2025, 12, 7);

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

        var handler = new GetCheckinsByDateQueryHandler(context, loggedUserContext);
        var query = new GetCheckinsByDateQuery(targetDate);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_SortsByHabitId()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var targetDate = new DateOnly(2025, 12, 7);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var habit1 = new Habit
        {
            UserId = userId,
            Title = "Habit A",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        var habit2 = new Habit
        {
            UserId = userId,
            Title = "Habit B",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        var habit3 = new Habit
        {
            UserId = userId,
            Title = "Habit C",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.AddRange(habit1, habit2, habit3);
        await context.SaveChangesAsync();

        // Add checkins in non-sorted order
        context.Checkins.AddRange(
            new Checkin
            {
                HabitId = habit2.Id,
                UserId = userId,
                LocalDate = targetDate,
                ActualValue = 1,
                TargetValueSnapshot = 1,
                CompletionModeSnapshot = CompletionMode.Binary,
                HabitTypeSnapshot = HabitType.Start,
                IsPlanned = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Checkin
            {
                HabitId = habit1.Id,
                UserId = userId,
                LocalDate = targetDate,
                ActualValue = 1,
                TargetValueSnapshot = 1,
                CompletionModeSnapshot = CompletionMode.Binary,
                HabitTypeSnapshot = HabitType.Start,
                IsPlanned = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Checkin
            {
                HabitId = habit3.Id,
                UserId = userId,
                LocalDate = targetDate,
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

        var handler = new GetCheckinsByDateQueryHandler(context, loggedUserContext);
        var query = new GetCheckinsByDateQuery(targetDate);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal(habit1.Id, result.Value[0].HabitId);
        Assert.Equal(habit2.Id, result.Value[1].HabitId);
        Assert.Equal(habit3.Id, result.Value[2].HabitId);
    }

    [Fact]
    public async Task Handle_ReturnsAllRequiredFields()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var targetDate = new DateOnly(2025, 12, 7);

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

        var handler = new GetCheckinsByDateQueryHandler(context, loggedUserContext);
        var query = new GetCheckinsByDateQuery(targetDate);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value);
        Assert.Equal(checkin.Id, item.Id);
        Assert.Equal(habit.Id, item.HabitId);
        Assert.Equal(targetDate, item.LocalDate);
        Assert.Equal(7, item.ActualValue);
        Assert.True(item.IsPlanned);
    }

    [Fact]
    public async Task Handle_OnlyReturnsCheckinsForCurrentUser()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var targetDate = new DateOnly(2025, 12, 7);

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

        var habit1 = new Habit
        {
            UserId = userId1,
            Title = "User1 Habit",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        var habit2 = new Habit
        {
            UserId = userId2,
            Title = "User2 Habit",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.AddRange(habit1, habit2);
        await context.SaveChangesAsync();

        // Checkin for user1
        context.Checkins.Add(new Checkin
        {
            HabitId = habit1.Id,
            UserId = userId1,
            LocalDate = targetDate,
            ActualValue = 1,
            TargetValueSnapshot = 1,
            CompletionModeSnapshot = CompletionMode.Binary,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        // Checkin for user2
        context.Checkins.Add(new Checkin
        {
            HabitId = habit2.Id,
            UserId = userId2,
            LocalDate = targetDate,
            ActualValue = 1,
            TargetValueSnapshot = 1,
            CompletionModeSnapshot = CompletionMode.Binary,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        // Act as user1
        var loggedUserContext = Substitute.For<ILoggedUserContext>();
        loggedUserContext.GetUser().Returns(new CurrentUser(userId1, "UTC", "user1@example.com"));

        var handler = new GetCheckinsByDateQueryHandler(context, loggedUserContext);
        var query = new GetCheckinsByDateQuery(targetDate);
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - only user1's checkin should be returned
        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value);
        Assert.Equal(habit1.Id, item.HabitId);
    }
}
