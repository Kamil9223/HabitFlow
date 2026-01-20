using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Core.Features.Notifications;
using HabitFlow.Core.Services;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class GetNotificationByIdQueryHandlerTests
{
    private readonly HabitFlowDbContext _dbContext;
    private readonly ILoggedUserContext _userContext;
    private readonly Guid _userId = Guid.NewGuid();

    public GetNotificationByIdQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HabitFlowDbContext(options);
        _userContext = Substitute.For<ILoggedUserContext>();
        _userContext.GetUser().Returns(x => new CurrentUser(_userId, "UTC", "user@test.pl"));
    }

    [Fact]
    public async Task Handle_ValidIdAndOwner_ReturnsNotification()
    {
        // Arrange
        var habit = new Habit
        {
            Id = 1,
            UserId = _userId,
            Title = "Morning Exercise",
            Description = "Daily morning workout",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);

        var notification = new Notification
        {
            Id = 123,
            UserId = _userId,
            HabitId = 1,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = NotificationType.MissDue,
            Content = "You missed your habit 'Morning Exercise' yesterday. Don't let one miss become a pattern!",
            AiStatus = AiGenerationStatus.Success,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1)
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationByIdQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationByIdQuery(123);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(123, result.Value.Id);
        Assert.Equal(1, result.Value.HabitId);
        Assert.Equal("Morning Exercise", result.Value.HabitName);
        Assert.Equal(NotificationType.MissDue, result.Value.Type);
        Assert.Equal("You missed your habit 'Morning Exercise' yesterday. Don't let one miss become a pattern!", result.Value.Content);
        Assert.Equal(AiGenerationStatus.Success, result.Value.AiStatus);
    }

    [Fact]
    public async Task Handle_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var handler = new GetNotificationByIdQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationByIdQuery(0);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("NOTIFICATION_NOT_FOUND", result.Error.Code);
        Assert.Equal(ErrorTitles.NotFound, result.Error.Title);
        Assert.Equal("Notification not found", result.Error.Description);
    }

    [Fact]
    public async Task Handle_NegativeId_ReturnsNotFound()
    {
        // Arrange
        var handler = new GetNotificationByIdQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationByIdQuery(-1);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("NOTIFICATION_NOT_FOUND", result.Error.Code);
        Assert.Equal(ErrorTitles.NotFound, result.Error.Title);
    }

    [Fact]
    public async Task Handle_NotificationNotExists_ReturnsNotFound()
    {
        // Arrange
        var handler = new GetNotificationByIdQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationByIdQuery(999);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("NOTIFICATION_NOT_FOUND", result.Error.Code);
        Assert.Equal(ErrorTitles.NotFound, result.Error.Title);
        Assert.Equal("Notification not found", result.Error.Description);
    }

    [Fact]
    public async Task Handle_NotificationBelongsToOtherUser_ReturnsNotFound()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var habit = new Habit
        {
            Id = 1,
            UserId = otherUserId,
            Title = "Other User Habit",
            Description = "Description",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);

        var notification = new Notification
        {
            Id = 456,
            UserId = otherUserId,
            HabitId = 1,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = NotificationType.MissDue,
            Content = "Other user's notification",
            AiStatus = AiGenerationStatus.Success,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationByIdQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationByIdQuery(456);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("NOTIFICATION_NOT_FOUND", result.Error.Code);
        Assert.Equal(ErrorTitles.NotFound, result.Error.Title);
    }

    [Fact]
    public async Task Handle_IncludesHabitName_ReturnsCorrectData()
    {
        // Arrange
        var habit = new Habit
        {
            Id = 2,
            UserId = _userId,
            Title = "Read Book",
            Description = "Read for 30 minutes",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127,
            TargetValue = 30,
            TargetUnit = "minutes",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);

        var notification = new Notification
        {
            Id = 789,
            UserId = _userId,
            HabitId = 2,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = NotificationType.MissDue,
            Content = "Keep your reading habit alive!",
            AiStatus = AiGenerationStatus.Fallback,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationByIdQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationByIdQuery(789);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Read Book", result.Value.HabitName);
        Assert.Equal(2, result.Value.HabitId);
    }

    [Fact]
    public async Task Handle_NullAiStatus_ReturnsNotificationWithNullAiStatus()
    {
        // Arrange
        var habit = new Habit
        {
            Id = 3,
            UserId = _userId,
            Title = "Meditation",
            Description = "Daily meditation",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);

        var notification = new Notification
        {
            Id = 111,
            UserId = _userId,
            HabitId = 3,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = NotificationType.MissDue,
            Content = "Notification without AI status",
            AiStatus = null,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationByIdQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationByIdQuery(111);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Null(result.Value.AiStatus);
    }

    [Fact]
    public async Task Handle_MultipleNotifications_ReturnsOnlyRequestedOne()
    {
        // Arrange
        var habit1 = new Habit
        {
            Id = 4,
            UserId = _userId,
            Title = "Habit 1",
            Description = "Description",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        var habit2 = new Habit
        {
            Id = 5,
            UserId = _userId,
            Title = "Habit 2",
            Description = "Description",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.AddRange(habit1, habit2);

        var notification1 = new Notification
        {
            Id = 100,
            UserId = _userId,
            HabitId = 4,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = NotificationType.MissDue,
            Content = "Notification 1",
            AiStatus = AiGenerationStatus.Success,
            CreatedAtUtc = DateTime.UtcNow
        };
        var notification2 = new Notification
        {
            Id = 200,
            UserId = _userId,
            HabitId = 5,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = NotificationType.MissDue,
            Content = "Notification 2",
            AiStatus = AiGenerationStatus.Success,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Notifications.AddRange(notification1, notification2);
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationByIdQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationByIdQuery(200);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(200, result.Value.Id);
        Assert.Equal("Notification 2", result.Value.Content);
        Assert.Equal("Habit 2", result.Value.HabitName);
    }
}
