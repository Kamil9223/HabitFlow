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

public class UpdateHabitCommandHandlerTests
{
    private readonly HabitFlowDbContext _dbContext;
    private readonly ILoggedUserContext _userContext;
    
    private readonly Guid _userId = Guid.NewGuid();
    public UpdateHabitCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new HabitFlowDbContext(options);
        _userContext = Substitute.For<ILoggedUserContext>();
        _userContext.GetUser().Returns(x => new CurrentUser(_userId, "UTC", "user-123@test.pl"));
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesHabitAndReturnsId()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userContext.GetUser().UserId,
            Title = "Old Title",
            Description = "Old Description",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            TargetUnit = "pages",
            DeadlineDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(_dbContext, _userContext);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            Title: "New Title",
            Description: "New Description",
            Type: HabitType.Stop,
            CompletionMode: CompletionMode.Quantitative,
            DaysOfWeekMask: 85,
            TargetValue: 20,
            TargetUnit: "minutes",
            DeadlineDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)),
            ClearDeadlineDate: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(habit.Id, result.Value);

        var updatedHabit = await _dbContext.Habits.FindAsync(habit.Id);
        Assert.NotNull(updatedHabit);
        Assert.Equal("New Title", updatedHabit.Title);
        Assert.Equal("New Description", updatedHabit.Description);
        Assert.Equal(HabitType.Stop, updatedHabit.Type);
        Assert.Equal(CompletionMode.Quantitative, updatedHabit.CompletionMode);
        Assert.Equal((byte)85, updatedHabit.DaysOfWeekMask);
        Assert.Equal((short)20, updatedHabit.TargetValue);
        Assert.Equal("minutes", updatedHabit.TargetUnit);
    }

    [Fact]
    public async Task Handle_PartialUpdate_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userContext.GetUser().UserId,
            Title = "Original Title",
            Description = "Original Description",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            TargetUnit = "pages",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(_dbContext, _userContext);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            Title: "Updated Title",
            Description: null,
            Type: null,
            CompletionMode: null,
            DaysOfWeekMask: null,
            TargetValue: null,
            TargetUnit: null,
            DeadlineDate: null,
            ClearDeadlineDate: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedHabit = await _dbContext.Habits.FindAsync(habit.Id);
        Assert.NotNull(updatedHabit);
        Assert.Equal("Updated Title", updatedHabit.Title);
        Assert.Equal("Original Description", updatedHabit.Description); // Unchanged
        Assert.Equal(HabitType.Start, updatedHabit.Type); // Unchanged
        Assert.Equal(CompletionMode.Binary, updatedHabit.CompletionMode); // Unchanged
    }

    [Fact]
    public async Task Handle_ClearDeadlineDate_SetsDeadlineToNull()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userContext.GetUser().UserId,
            Title = "Title",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            DeadlineDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(_dbContext, _userContext);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            Title: null,
            Description: null,
            Type: null,
            CompletionMode: null,
            DaysOfWeekMask: null,
            TargetValue: null,
            TargetUnit: null,
            DeadlineDate: null,
            ClearDeadlineDate: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedHabit = await _dbContext.Habits.FindAsync(habit.Id);
        Assert.NotNull(updatedHabit);
        Assert.Null(updatedHabit.DeadlineDate);
    }

    [Fact]
    public async Task Handle_NonExistentHabit_ReturnsNotFoundError()
    {
        // Arrange
        var handler = new UpdateHabitCommandHandler(_dbContext, _userContext);
        var command = new UpdateHabitCommand(
            Id: 999,
            Title: "New Title",
            Description: null,
            Type: null,
            CompletionMode: null,
            DaysOfWeekMask: null,
            TargetValue: null,
            TargetUnit: null,
            DeadlineDate: null,
            ClearDeadlineDate: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_HabitBelongsToOtherUser_ReturnsNotFoundError()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = Guid.NewGuid(), // Different user
            Title = "Title",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(_dbContext, _userContext);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            Title: "Hacked Title",
            Description: null,
            Type: null,
            CompletionMode: null,
            DaysOfWeekMask: null,
            TargetValue: null,
            TargetUnit: null,
            DeadlineDate: null,
            ClearDeadlineDate: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.NotFound", result.Error.Code);

        // Verify habit was not modified
        var unchangedHabit = await _dbContext.Habits.FindAsync(habit.Id);
        Assert.NotNull(unchangedHabit);
        Assert.Equal("Title", unchangedHabit.Title);
    }

    [Fact]
    public async Task Handle_EmptyTitle_ReturnsValidationError()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = Guid.NewGuid(),
            Title = "Original Title",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(_dbContext, _userContext);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            Title: "",
            Description: null,
            Type: null,
            CompletionMode: null,
            DaysOfWeekMask: null,
            TargetValue: null,
            TargetUnit: null,
            DeadlineDate: null,
            ClearDeadlineDate: false);

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
        var habit = new Habit
        {
            UserId = Guid.NewGuid(),
            Title = "Original Title",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(_dbContext, _userContext);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            Title: new string('A', 81),
            Description: null,
            Type: null,
            CompletionMode: null,
            DaysOfWeekMask: null,
            TargetValue: null,
            TargetUnit: null,
            DeadlineDate: null,
            ClearDeadlineDate: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.TitleTooLong", result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Handle_InvalidTargetValue_ReturnsValidationError(short invalidValue)
    {
        // Arrange
        var habit = new Habit
        {
            UserId = Guid.NewGuid(),
            Title = "Title",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(_dbContext, _userContext);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            Title: null,
            Description: null,
            Type: null,
            CompletionMode: null,
            DaysOfWeekMask: null,
            TargetValue: invalidValue,
            TargetUnit: null,
            DeadlineDate: null,
            ClearDeadlineDate: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.InvalidTargetValue", result.Error.Code);
    }

    [Fact]
    public async Task Handle_DeadlineDateInPast_ReturnsValidationError()
    {
        // Arrange ;
        var habit = new Habit
        {
            UserId = Guid.NewGuid(),
            Title = "Title",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(_dbContext, _userContext);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            Title: null,
            Description: null,
            Type: null,
            CompletionMode: null,
            DaysOfWeekMask: null,
            TargetValue: null,
            TargetUnit: null,
            DeadlineDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            ClearDeadlineDate: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.InvalidDeadlineDate", result.Error.Code);
    }

    [Fact]
    public async Task Handle_DeadlineDateAndClearDeadlineBothSet_ReturnsValidationError()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = Guid.NewGuid(),
            Title = "Title",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(_dbContext, _userContext);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            Title: null,
            Description: null,
            Type: null,
            CompletionMode: null,
            DaysOfWeekMask: null,
            TargetValue: null,
            TargetUnit: null,
            DeadlineDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            ClearDeadlineDate: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.DeadlineConflict", result.Error.Code);
    }
}
