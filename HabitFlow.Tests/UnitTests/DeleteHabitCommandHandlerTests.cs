using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Features.Habits;
using HabitFlow.Core.Services;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class DeleteHabitCommandHandlerTests
{
    private readonly HabitFlowDbContext _dbContext;
    private readonly ILoggedUserContext _userContext;
    
    private readonly Guid _userId = Guid.NewGuid();
    
    public DeleteHabitCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new HabitFlowDbContext(options);
        _userContext = NSubstitute.Substitute.For<ILoggedUserContext>();
        _userContext.GetUser().Returns(x => new CurrentUser(_userId, "UTC", "user-123@test.pl"));
    }

    [Fact]
    public async Task Handle_ValidHabitId_ReturnsSuccessAndDeletesHabit()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteHabitCommandHandler(_dbContext, _userContext);
        var command = new DeleteHabitCommand(habit.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var deletedHabit = await _dbContext.Habits.FirstOrDefaultAsync(h => h.Id == habit.Id);
        Assert.Null(deletedHabit);
    }

    [Fact]
    public async Task Handle_NonExistentHabitId_ReturnsNotFoundError()
    {
        // Arrange
        var handler = new DeleteHabitCommandHandler(_dbContext, _userContext);
        var command = new DeleteHabitCommand(999);

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
        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var habit = new Habit
        {
            UserId = ownerUserId,
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteHabitCommandHandler(_dbContext, _userContext);
        var command = new DeleteHabitCommand(habit.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.NotFound", result.Error.Code);

        // Verify habit still exists
        var stillExists = await _dbContext.Habits.AnyAsync(h => h.Id == habit.Id);
        Assert.True(stillExists);
    }

    [Fact]
    public async Task Handle_HabitWithCheckins_DeletesHabitAndCascadesCheckins()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        // Add checkins for the habit
        var checkin1 = new Checkin
        {
            HabitId = habit.Id,
            UserId = _userId,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue = 5,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = CompletionMode.Binary,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var checkin2 = new Checkin
        {
            HabitId = habit.Id,
            UserId = _userId,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            ActualValue = 8,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = CompletionMode.Binary,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Checkins.AddRange(checkin1, checkin2);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteHabitCommandHandler(_dbContext, _userContext);
        var command = new DeleteHabitCommand(habit.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify habit is deleted
        var deletedHabit = await _dbContext.Habits.FirstOrDefaultAsync(h => h.Id == habit.Id);
        Assert.Null(deletedHabit);

        // Verify checkins are cascaded (deleted)
        var remainingCheckins = await _dbContext.Checkins.Where(c => c.HabitId == habit.Id).ToListAsync();
        Assert.Empty(remainingCheckins);
    }

    [Fact]
    public async Task Handle_HabitWithNotifications_DeletesHabitAndCascadesNotifications()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        // Add notifications for the habit
        var notification = new Notification
        {
            UserId = _userId,
            HabitId = habit.Id,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            Type = NotificationType.MissDue,
            Content = "You missed yesterday!",
            AiStatus = AiGenerationStatus.Success,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteHabitCommandHandler(_dbContext, _userContext);
        var command = new DeleteHabitCommand(habit.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify habit is deleted
        var deletedHabit = await _dbContext.Habits.FirstOrDefaultAsync(h => h.Id == habit.Id);
        Assert.Null(deletedHabit);

        // Verify notifications are cascaded (deleted)
        var remainingNotifications = await _dbContext.Notifications.Where(n => n.HabitId == habit.Id).ToListAsync();
        Assert.Empty(remainingNotifications);
    }
}
