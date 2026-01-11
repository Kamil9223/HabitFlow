using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Features.Profile;
using HabitFlow.Core.Services;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class GetProfileQueryHandlerTests
{
    private readonly HabitFlowDbContext _dbContext;
    private readonly ILoggedUserContext _userContext;

    private readonly Guid _userId = Guid.NewGuid();

    public GetProfileQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HabitFlowDbContext(options);
        _userContext = Substitute.For<ILoggedUserContext>();
        _userContext.GetUser().Returns(new CurrentUser(_userId, "Europe/Warsaw", "test@example.com"));
    }

    [Fact]
    public async Task Handle_ValidUser_ReturnsSuccessWithUserData()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = _userId,
            UserName = "test@example.com",
            Email = "test@example.com",
            EmailConfirmed = true,
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var handler = new GetProfileQueryHandler(_userContext, _dbContext);
        var query = new GetProfileQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(_userId, result.Value.UserId);
        Assert.Equal("test@example.com", result.Value.Email);
        Assert.True(result.Value.EmailConfirmed);
        Assert.Equal("Europe/Warsaw", result.Value.TimeZoneId);
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero), result.Value.CreatedAtUtc);
        Assert.Equal(0, result.Value.HabitsCount);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFoundError()
    {
        // Arrange - No user in database
        var handler = new GetProfileQueryHandler(_userContext, _dbContext);
        var query = new GetProfileQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
        Assert.Equal("User account not found.", result.Error.Description);
    }

    [Fact]
    public async Task Handle_EmailNotConfirmed_ReturnsUserDataWithCorrectFlag()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = _userId,
            UserName = "test@example.com",
            Email = "test@example.com",
            EmailConfirmed = false,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var handler = new GetProfileQueryHandler(_userContext, _dbContext);
        var query = new GetProfileQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.EmailConfirmed);
    }

    [Fact]
    public async Task Handle_DifferentTimeZone_MapsTimeZoneCorrectly()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = _userId,
            UserName = "test@example.com",
            Email = "test@example.com",
            EmailConfirmed = true,
            TimeZoneId = "America/New_York",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var handler = new GetProfileQueryHandler(_userContext, _dbContext);
        var query = new GetProfileQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("America/New_York", result.Value.TimeZoneId);
    }

    [Fact]
    public async Task Handle_CreatedAtUtcMapping_ConvertsToDateTimeOffsetCorrectly()
    {
        // Arrange
        var createdAt = new DateTime(2024, 6, 15, 10, 30, 45, DateTimeKind.Utc);
        var user = new ApplicationUser
        {
            Id = _userId,
            UserName = "test@example.com",
            Email = "test@example.com",
            EmailConfirmed = true,
            TimeZoneId = "Europe/London",
            CreatedAtUtc = createdAt
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var handler = new GetProfileQueryHandler(_userContext, _dbContext);
        var query = new GetProfileQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var expectedOffset = new DateTimeOffset(createdAt, TimeSpan.Zero);
        Assert.Equal(expectedOffset, result.Value.CreatedAtUtc);
        Assert.Equal(0, result.Value.CreatedAtUtc.Offset.TotalHours);
    }

    [Fact]
    public async Task Handle_WithHabits_ReturnsCorrectHabitsCount()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = _userId,
            UserName = "test@example.com",
            Email = "test@example.com",
            EmailConfirmed = true,
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);

        // Add 3 habits for the user
        _dbContext.Habits.Add(new Habit
        {
            UserId = _userId,
            Title = "Habit 1",
            Type = Data.Enums.HabitType.Start,
            CompletionMode = Data.Enums.CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        });
        _dbContext.Habits.Add(new Habit
        {
            UserId = _userId,
            Title = "Habit 2",
            Type = Data.Enums.HabitType.Stop,
            CompletionMode = Data.Enums.CompletionMode.Quantitative,
            DaysOfWeekMask = 85,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        });
        _dbContext.Habits.Add(new Habit
        {
            UserId = _userId,
            Title = "Habit 3",
            Type = Data.Enums.HabitType.Start,
            CompletionMode = Data.Enums.CompletionMode.Binary,
            DaysOfWeekMask = 31,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        var handler = new GetProfileQueryHandler(_userContext, _dbContext);
        var query = new GetProfileQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.HabitsCount);
    }
}
