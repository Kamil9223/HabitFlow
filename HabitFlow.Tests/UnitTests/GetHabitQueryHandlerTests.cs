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

public class GetHabitQueryHandlerTests
{
    private readonly HabitFlowDbContext _dbContext;
    private readonly ILoggedUserContext _userContext;

    private readonly Guid _userId = Guid.NewGuid();

    public GetHabitQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HabitFlowDbContext(options);
        _userContext = Substitute.For<ILoggedUserContext>();
        _userContext.GetUser().Returns(x => new CurrentUser(_userId, "UTC", "user-123@test.pl"));
    }

    [Fact]
    public async Task Handle_ValidQueryForExistingHabit_ReturnsSuccessWithHabitDto()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Read books",
            Description = "Read 10 pages daily",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            TargetUnit = "pages",
            DeadlineDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitQueryHandler(_dbContext, _userContext);
        var query = new GetHabitQuery(habit.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(habit.Id, result.Value.Id);
        Assert.Equal("Read books", result.Value.Title);
        Assert.Equal("Read 10 pages daily", result.Value.Description);
        Assert.Equal(HabitType.Start, result.Value.Type);
        Assert.Equal(CompletionMode.Quantitative, result.Value.CompletionMode);
        Assert.Equal((byte)127, result.Value.DaysOfWeekMask);
        Assert.Equal((short)10, result.Value.TargetValue);
        Assert.Equal("pages", result.Value.TargetUnit);
        Assert.NotNull(result.Value.DeadlineDate);
    }

    [Fact]
    public async Task Handle_HabitDoesNotExist_ReturnsNotFoundError()
    {
        // Arrange
        var handler = new GetHabitQueryHandler(_dbContext, _userContext);
        var query = new GetHabitQuery(999);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_HabitBelongsToDifferentUser_ReturnsNotFoundError()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = Guid.NewGuid(),
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitQueryHandler(_dbContext, _userContext);
        var query = new GetHabitQuery(habit.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.NotFound", result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task Handle_InvalidHabitId_ReturnsValidationError(int invalidId)
    {
        // Arrange
        var handler = new GetHabitQueryHandler(_dbContext, _userContext);
        var query = new GetHabitQuery(invalidId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.InvalidId", result.Error.Code);
    }

    [Fact]
    public async Task Handle_HabitWithMinimalFields_ReturnsSuccessWithNullOptionalFields()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Simple habit",
            Description = null,
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 1,
            TargetValue = 1,
            TargetUnit = null,
            DeadlineDate = null,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitQueryHandler(_dbContext, _userContext);
        var query = new GetHabitQuery(habit.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Simple habit", result.Value.Title);
        Assert.Null(result.Value.Description);
        Assert.Null(result.Value.TargetUnit);
        Assert.Null(result.Value.DeadlineDate);
    }

    [Fact]
    public async Task Handle_MultipleHabitsForUser_ReturnsCorrectHabit()
    {
        // Arrange ;
        var habit1 = new Habit
        {
            UserId = _userId,
            Title = "Habit 1",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        var habit2 = new Habit
        {
            UserId = _userId,
            Title = "Habit 2",
            Type = HabitType.Stop,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 63,
            TargetValue = 5,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Habits.AddRange(habit1, habit2);
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitQueryHandler(_dbContext, _userContext);
        var query = new GetHabitQuery(habit2.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(habit2.Id, result.Value.Id);
        Assert.Equal("Habit 2", result.Value.Title);
        Assert.Equal(HabitType.Stop, result.Value.Type);
        Assert.Equal(CompletionMode.Quantitative, result.Value.CompletionMode);
        Assert.Equal((byte)63, result.Value.DaysOfWeekMask);
        Assert.Equal((short)5, result.Value.TargetValue);
    }
}
