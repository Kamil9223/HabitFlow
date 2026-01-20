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

public class GetNotificationsQueryHandlerTests
{
    private readonly HabitFlowDbContext _dbContext;
    private readonly ILoggedUserContext _userContext;
    private readonly Guid _userId = Guid.NewGuid();

    public GetNotificationsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HabitFlowDbContext(options);
        _userContext = Substitute.For<ILoggedUserContext>();
        _userContext.GetUser().Returns(x => new CurrentUser(_userId, "UTC", "user@test.pl"));
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsPagedNotifications()
    {
        // Arrange
        var notification1 = new Notification
        {
            UserId = _userId,
            HabitId = 1,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = NotificationType.MissDue,
            Content = "You missed yesterday's check-in",
            AiStatus = AiGenerationStatus.Success,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2)
        };
        var notification2 = new Notification
        {
            UserId = _userId,
            HabitId = 2,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            Type = NotificationType.MissDue,
            Content = "Keep going! Don't break the streak",
            AiStatus = AiGenerationStatus.Fallback,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1)
        };

        _dbContext.Notifications.AddRange(notification1, notification2);
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationsQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationsQuery(Page: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task Handle_EmptyDatabase_ReturnsEmptyPagedResult()
    {
        // Arrange
        var handler = new GetNotificationsQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationsQuery(Page: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            _dbContext.Notifications.Add(new Notification
            {
                UserId = _userId,
                HabitId = 1,
                LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Type = NotificationType.MissDue,
                Content = $"Notification {i}",
                AiStatus = AiGenerationStatus.Success,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-i)
            });
        }
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationsQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationsQuery(Page: 2, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(25, result.Value.TotalCount);
        Assert.Equal(10, result.Value.Items.Count);
    }

    [Fact]
    public async Task Handle_PageSizeExceedsMax_ClampsToMaxPageSize()
    {
        // Arrange
        for (int i = 1; i <= 150; i++)
        {
            _dbContext.Notifications.Add(new Notification
            {
                UserId = _userId,
                HabitId = 1,
                LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Type = NotificationType.MissDue,
                Content = $"Notification {i}",
                AiStatus = AiGenerationStatus.Success,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationsQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationsQuery(Page: 1, PageSize: 200);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(150, result.Value.TotalCount);
        Assert.Equal(100, result.Value.Items.Count); // Clamped to max 100
    }

    [Fact]
    public async Task Handle_SortByCreatedAtUtcDesc_ReturnsSortedNotifications()
    {
        // Arrange
        _dbContext.Notifications.AddRange(
            new Notification { UserId = _userId, HabitId = 1, LocalDate = DateOnly.FromDateTime(DateTime.UtcNow), Type = NotificationType.MissDue, Content = "Oldest", AiStatus = AiGenerationStatus.Success, CreatedAtUtc = DateTime.UtcNow.AddDays(-10) },
            new Notification { UserId = _userId, HabitId = 1, LocalDate = DateOnly.FromDateTime(DateTime.UtcNow), Type = NotificationType.MissDue, Content = "Newest", AiStatus = AiGenerationStatus.Success, CreatedAtUtc = DateTime.UtcNow },
            new Notification { UserId = _userId, HabitId = 1, LocalDate = DateOnly.FromDateTime(DateTime.UtcNow), Type = NotificationType.MissDue, Content = "Middle", AiStatus = AiGenerationStatus.Success, CreatedAtUtc = DateTime.UtcNow.AddDays(-5) }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationsQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationsQuery(SortField: NotificationSortField.CreatedAtUtc, SortDirection: SortDirection.Desc);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal("Newest", result.Value.Items[0].Content);
        Assert.Equal("Middle", result.Value.Items[1].Content);
        Assert.Equal("Oldest", result.Value.Items[2].Content);
    }

    [Fact]
    public async Task Handle_SortByLocalDateAsc_ReturnsSortedNotifications()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _dbContext.Notifications.AddRange(
            new Notification { UserId = _userId, HabitId = 1, LocalDate = today.AddDays(-5), Type = NotificationType.MissDue, Content = "Oldest date", AiStatus = AiGenerationStatus.Success, CreatedAtUtc = DateTime.UtcNow },
            new Notification { UserId = _userId, HabitId = 1, LocalDate = today, Type = NotificationType.MissDue, Content = "Newest date", AiStatus = AiGenerationStatus.Success, CreatedAtUtc = DateTime.UtcNow },
            new Notification { UserId = _userId, HabitId = 1, LocalDate = today.AddDays(-2), Type = NotificationType.MissDue, Content = "Middle date", AiStatus = AiGenerationStatus.Success, CreatedAtUtc = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationsQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationsQuery(SortField: NotificationSortField.LocalDate, SortDirection: SortDirection.Asc);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal("Oldest date", result.Value.Items[0].Content);
        Assert.Equal("Middle date", result.Value.Items[1].Content);
        Assert.Equal("Newest date", result.Value.Items[2].Content);
    }

    [Fact]
    public async Task Handle_SortByTypeDesc_ReturnsSortedNotifications()
    {
        // Arrange
        _dbContext.Notifications.AddRange(
            new Notification { UserId = _userId, HabitId = 1, LocalDate = DateOnly.FromDateTime(DateTime.UtcNow), Type = NotificationType.MissDue, Content = "MissDue notification", AiStatus = AiGenerationStatus.Success, CreatedAtUtc = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationsQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationsQuery(SortField: NotificationSortField.Type, SortDirection: SortDirection.Desc);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(NotificationType.MissDue, result.Value.Items[0].Type);
    }

    [Fact]
    public async Task Handle_UserIsolation_ReturnsOnlyCurrentUserNotifications()
    {
        // Arrange
        var user2 = Guid.NewGuid();
        _dbContext.Notifications.AddRange(
            new Notification { UserId = _userId, HabitId = 1, LocalDate = DateOnly.FromDateTime(DateTime.UtcNow), Type = NotificationType.MissDue, Content = "User 1 notification 1", AiStatus = AiGenerationStatus.Success, CreatedAtUtc = DateTime.UtcNow },
            new Notification { UserId = user2, HabitId = 2, LocalDate = DateOnly.FromDateTime(DateTime.UtcNow), Type = NotificationType.MissDue, Content = "User 2 notification", AiStatus = AiGenerationStatus.Success, CreatedAtUtc = DateTime.UtcNow },
            new Notification { UserId = _userId, HabitId = 3, LocalDate = DateOnly.FromDateTime(DateTime.UtcNow), Type = NotificationType.MissDue, Content = "User 1 notification 2", AiStatus = AiGenerationStatus.Success, CreatedAtUtc = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationsQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Contains(result.Value.Items, n => n.Content == "User 1 notification 1");
        Assert.Contains(result.Value.Items, n => n.Content == "User 1 notification 2");
        Assert.DoesNotContain(result.Value.Items, n => n.Content == "User 2 notification");
    }

    [Fact]
    public async Task Handle_NullAiStatus_ReturnsNotificationWithNullAiStatus()
    {
        // Arrange
        _dbContext.Notifications.Add(new Notification
        {
            UserId = _userId,
            HabitId = 1,
            LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = NotificationType.MissDue,
            Content = "Notification without AI status",
            AiStatus = null,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationsQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Null(result.Value.Items[0].AiStatus);
    }

    [Fact]
    public async Task Handle_PageOutOfRange_ReturnsEmptyList()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            _dbContext.Notifications.Add(new Notification
            {
                UserId = _userId,
                HabitId = 1,
                LocalDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Type = NotificationType.MissDue,
                Content = $"Notification {i}",
                AiStatus = AiGenerationStatus.Success,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();

        var handler = new GetNotificationsQueryHandler(_dbContext, _userContext);
        var query = new GetNotificationsQuery(Page: 999, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(10, result.Value.TotalCount); // Total count should still be 10
        Assert.Empty(result.Value.Items); // But items should be empty for page out of range
    }
}
