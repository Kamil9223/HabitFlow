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

public class CreateHabitCommandHandlerTests
{
    private readonly HabitFlowDbContext _dbContext;
    private readonly ILoggedUserContext _userContext;
    
    private readonly Guid _userId = Guid.NewGuid();
    
    public CreateHabitCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new HabitFlowDbContext(options);
        _userContext = NSubstitute.Substitute.For<ILoggedUserContext>();
        _userContext.GetUser().Returns(x => new CurrentUser(_userId, "UTC", "user-123@test.pl"));
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithHabitId()
    {
        // Arrange
        var handler = new CreateHabitCommandHandler(_dbContext, _userContext);
        var command = new CreateHabitCommand(
            Title: "Read books",
            Description: "Read 10 pages daily",
            Type: HabitType.Start,
            CompletionMode: CompletionMode.Quantitative,
            DaysOfWeekMask: 127,
            TargetValue: 10,
            TargetUnit: "pages",
            DeadlineDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);

        var habit = await _dbContext.Habits.FirstOrDefaultAsync(h => h.Id == result.Value);
        Assert.NotNull(habit);
        Assert.Equal("Read books", habit.Title);
        Assert.Equal(_userId, habit.UserId);
    }

    [Fact]
    public async Task Handle_EmptyTitle_ReturnsValidationError()
    {
        // Arrange
        var handler = new CreateHabitCommandHandler(_dbContext, _userContext);
        var command = new CreateHabitCommand(
            Title: "",
            Description: null,
            Type: HabitType.Start,
            CompletionMode: CompletionMode.Binary,
            DaysOfWeekMask: 127,
            TargetValue: 1,
            TargetUnit: null,
            DeadlineDate: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.TitleRequired", result.Error.Code);
    }

    [Fact]
    public async Task Handle_TitleTooLong_ReturnsValidationError()
    {
        // Arrange
        var handler = new CreateHabitCommandHandler(_dbContext, _userContext);
        var command = new CreateHabitCommand(
            Title: new string('A', 81),
            Description: null,
            Type: HabitType.Start,
            CompletionMode: CompletionMode.Binary,
            DaysOfWeekMask: 127,
            TargetValue: 1,
            TargetUnit: null,
            DeadlineDate: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.TitleTooLong", result.Error.Code);
    }

    [Fact]
    public async Task Handle_DescriptionTooLong_ReturnsValidationError()
    {
        // Arrange
        var handler = new CreateHabitCommandHandler(_dbContext, _userContext);
        var command = new CreateHabitCommand(
            Title: "Valid Title",
            Description: new string('A', 281),
            Type: HabitType.Start,
            CompletionMode: CompletionMode.Binary,
            DaysOfWeekMask: 127,
            TargetValue: 1,
            TargetUnit: null,
            DeadlineDate: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.DescriptionTooLong", result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(128)]
    public async Task Handle_InvalidDaysOfWeekMask_ReturnsValidationError(byte invalidMask)
    {
        // Arrange
        var handler = new CreateHabitCommandHandler(_dbContext, _userContext);
        var command = new CreateHabitCommand(
            Title: "Valid Title",
            Description: null,
            Type: HabitType.Start,
            CompletionMode: CompletionMode.Binary,
            DaysOfWeekMask: invalidMask,
            TargetValue: 1,
            TargetUnit: null,
            DeadlineDate: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.InvalidDaysOfWeekMask", result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Handle_InvalidTargetValue_ReturnsValidationError(short invalidValue)
    {
        // Arrange
        var handler = new CreateHabitCommandHandler(_dbContext, _userContext);
        var command = new CreateHabitCommand(
            Title: "Valid Title",
            Description: null,
            Type: HabitType.Start,
            CompletionMode: CompletionMode.Binary,
            DaysOfWeekMask: 127,
            TargetValue: invalidValue,
            TargetUnit: null,
            DeadlineDate: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.InvalidTargetValue", result.Error.Code);
    }

    [Fact]
    public async Task Handle_TargetUnitTooLong_ReturnsValidationError()
    {
        // Arrange
        var handler = new CreateHabitCommandHandler(_dbContext, _userContext);
        var command = new CreateHabitCommand(
            Title: "Valid Title",
            Description: null,
            Type: HabitType.Start,
            CompletionMode: CompletionMode.Binary,
            DaysOfWeekMask: 127,
            TargetValue: 10,
            TargetUnit: new string('A', 33),
            DeadlineDate: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.TargetUnitTooLong", result.Error.Code);
    }

    [Fact]
    public async Task Handle_DeadlineDateInPast_ReturnsValidationError()
    {
        // Arrange
        var handler = new CreateHabitCommandHandler(_dbContext, _userContext);
        var command = new CreateHabitCommand(
            Title: "Valid Title",
            Description: null,
            Type: HabitType.Start,
            CompletionMode: CompletionMode.Binary,
            DaysOfWeekMask: 127,
            TargetValue: 10,
            TargetUnit: null,
            DeadlineDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.InvalidDeadlineDate", result.Error.Code);
    }

    [Fact]
    public async Task Handle_UserHas20Habits_ReturnsConflictError()
    {
        // Arrange
        // Seed 20 habits for the user
        for (int i = 1; i <= 20; i++)
        {
            _dbContext.Habits.Add(new Habit
            {
                UserId = _userId,
                Title = $"Habit {i}",
                Type = HabitType.Start,
                CompletionMode = CompletionMode.Binary,
                DaysOfWeekMask = 127,
                TargetValue = 1,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();

        var handler = new CreateHabitCommandHandler(_dbContext, _userContext);
        var command = new CreateHabitCommand(
            Title: "Habit 21",
            Description: null,
            Type: HabitType.Start,
            CompletionMode: CompletionMode.Binary,
            DaysOfWeekMask: 127,
            TargetValue: 1,
            TargetUnit: null,
            DeadlineDate: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.LimitExceeded", result.Error.Code);
    }

    [Fact]
    public async Task Handle_UserHas19Habits_SuccessfullyCreates20th()
    {
        // Arrange
        // Seed 19 habits for the user
        for (int i = 1; i <= 19; i++)
        {
            _dbContext.Habits.Add(new Habit
            {
                UserId = _userId,
                Title = $"Habit {i}",
                Type = HabitType.Start,
                CompletionMode = CompletionMode.Binary,
                DaysOfWeekMask = 127,
                TargetValue = 1,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();

        var handler = new CreateHabitCommandHandler(_dbContext, _userContext);
        var command = new CreateHabitCommand(
            Title: "Habit 20",
            Description: null,
            Type: HabitType.Start,
            CompletionMode: CompletionMode.Binary,
            DaysOfWeekMask: 127,
            TargetValue: 1,
            TargetUnit: null,
            DeadlineDate: null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(20, await _dbContext.Habits.CountAsync(h => h.UserId == _userContext.GetUser().UserId));
    }
}
