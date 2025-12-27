using HabitFlow.Core.Features.Habits;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class DeleteHabitCommandHandlerTests
{
    private HabitFlowDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HabitFlowDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidHabitId_ReturnsSuccessAndDeletesHabit()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";
        var habit = new Habit
        {
            UserId = userId,
            Title = "Read books",
            Type = 1,
            CompletionMode = 1,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var handler = new DeleteHabitCommandHandler(context);
        var command = new DeleteHabitCommand(habit.Id, userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var deletedHabit = await context.Habits.FirstOrDefaultAsync(h => h.Id == habit.Id);
        Assert.Null(deletedHabit);
    }

    [Fact]
    public async Task Handle_NonExistentHabitId_ReturnsNotFoundError()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var handler = new DeleteHabitCommandHandler(context);
        var command = new DeleteHabitCommand(999, "user-123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_HabitBelongsToAnotherUser_ReturnsNotFoundError()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var ownerUserId = "user-123";
        var otherUserId = "user-456";
        var habit = new Habit
        {
            UserId = ownerUserId,
            Title = "Read books",
            Type = 1,
            CompletionMode = 1,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var handler = new DeleteHabitCommandHandler(context);
        var command = new DeleteHabitCommand(habit.Id, otherUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.NotFound", result.Error.Code);

        // Verify habit still exists
        var stillExists = await context.Habits.AnyAsync(h => h.Id == habit.Id);
        Assert.True(stillExists);
    }

    [Fact]
    public async Task Handle_HabitWithCheckins_DeletesHabitAndCascadesCheckins()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";
        var habit = new Habit
        {
            UserId = userId,
            Title = "Read books",
            Type = 1,
            CompletionMode = 1,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        // Add checkins for the habit
        var checkin1 = new Checkin
        {
            HabitId = habit.Id,
            UserId = userId,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue = 5,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = 1,
            HabitTypeSnapshot = 1,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var checkin2 = new Checkin
        {
            HabitId = habit.Id,
            UserId = userId,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            ActualValue = 8,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = 1,
            HabitTypeSnapshot = 1,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Checkins.AddRange(checkin1, checkin2);
        await context.SaveChangesAsync();

        var handler = new DeleteHabitCommandHandler(context);
        var command = new DeleteHabitCommand(habit.Id, userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify habit is deleted
        var deletedHabit = await context.Habits.FirstOrDefaultAsync(h => h.Id == habit.Id);
        Assert.Null(deletedHabit);

        // Verify checkins are cascaded (deleted)
        var remainingCheckins = await context.Checkins.Where(c => c.HabitId == habit.Id).ToListAsync();
        Assert.Empty(remainingCheckins);
    }

    [Fact]
    public async Task Handle_HabitWithNotifications_DeletesHabitAndCascadesNotifications()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";
        var habit = new Habit
        {
            UserId = userId,
            Title = "Read books",
            Type = 1,
            CompletionMode = 1,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        // Add notifications for the habit
        var notification = new Notification
        {
            UserId = userId,
            HabitId = habit.Id,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            Type = 1,
            Content = "You missed yesterday!",
            AiStatus = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var handler = new DeleteHabitCommandHandler(context);
        var command = new DeleteHabitCommand(habit.Id, userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify habit is deleted
        var deletedHabit = await context.Habits.FirstOrDefaultAsync(h => h.Id == habit.Id);
        Assert.Null(deletedHabit);

        // Verify notifications are cascaded (deleted)
        var remainingNotifications = await context.Notifications.Where(n => n.HabitId == habit.Id).ToListAsync();
        Assert.Empty(remainingNotifications);
    }
}
