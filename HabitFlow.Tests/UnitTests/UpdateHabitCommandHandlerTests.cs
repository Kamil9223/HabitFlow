using HabitFlow.Core.Features.Habits;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class UpdateHabitCommandHandlerTests
{
    private HabitFlowDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HabitFlowDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesHabitAndReturnsId()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var habit = new Habit
        {
            UserId = "user-123",
            Title = "Old Title",
            Description = "Old Description",
            Type = 1,
            CompletionMode = 1,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            TargetUnit = "pages",
            DeadlineDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(context);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            UserId: "user-123",
            Title: "New Title",
            Description: "New Description",
            Type: 2,
            CompletionMode: 2,
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

        var updatedHabit = await context.Habits.FindAsync(habit.Id);
        Assert.NotNull(updatedHabit);
        Assert.Equal("New Title", updatedHabit.Title);
        Assert.Equal("New Description", updatedHabit.Description);
        Assert.Equal((byte)2, updatedHabit.Type);
        Assert.Equal((byte)2, updatedHabit.CompletionMode);
        Assert.Equal((byte)85, updatedHabit.DaysOfWeekMask);
        Assert.Equal((short)20, updatedHabit.TargetValue);
        Assert.Equal("minutes", updatedHabit.TargetUnit);
    }

    [Fact]
    public async Task Handle_PartialUpdate_UpdatesOnlyProvidedFields()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var habit = new Habit
        {
            UserId = "user-123",
            Title = "Original Title",
            Description = "Original Description",
            Type = 1,
            CompletionMode = 1,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            TargetUnit = "pages",
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(context);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            UserId: "user-123",
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

        var updatedHabit = await context.Habits.FindAsync(habit.Id);
        Assert.NotNull(updatedHabit);
        Assert.Equal("Updated Title", updatedHabit.Title);
        Assert.Equal("Original Description", updatedHabit.Description); // Unchanged
        Assert.Equal((byte)1, updatedHabit.Type); // Unchanged
        Assert.Equal((byte)1, updatedHabit.CompletionMode); // Unchanged
    }

    [Fact]
    public async Task Handle_ClearDeadlineDate_SetsDeadlineToNull()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var habit = new Habit
        {
            UserId = "user-123",
            Title = "Title",
            Type = 1,
            CompletionMode = 1,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            DeadlineDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(context);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            UserId: "user-123",
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

        var updatedHabit = await context.Habits.FindAsync(habit.Id);
        Assert.NotNull(updatedHabit);
        Assert.Null(updatedHabit.DeadlineDate);
    }

    [Fact]
    public async Task Handle_NonExistentHabit_ReturnsNotFoundError()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var handler = new UpdateHabitCommandHandler(context);
        var command = new UpdateHabitCommand(
            Id: 999,
            UserId: "user-123",
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
        await using var context = CreateInMemoryContext();
        var habit = new Habit
        {
            UserId = "user-456", // Different user
            Title = "Title",
            Type = 1,
            CompletionMode = 1,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(context);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            UserId: "user-123", // Different user trying to update
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
        var unchangedHabit = await context.Habits.FindAsync(habit.Id);
        Assert.NotNull(unchangedHabit);
        Assert.Equal("Title", unchangedHabit.Title);
    }

    [Fact]
    public async Task Handle_EmptyTitle_ReturnsValidationError()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var habit = new Habit
        {
            UserId = "user-123",
            Title = "Original Title",
            Type = 1,
            CompletionMode = 1,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(context);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            UserId: "user-123",
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
        await using var context = CreateInMemoryContext();
        var habit = new Habit
        {
            UserId = "user-123",
            Title = "Original Title",
            Type = 1,
            CompletionMode = 1,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(context);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            UserId: "user-123",
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
        await using var context = CreateInMemoryContext();
        var habit = new Habit
        {
            UserId = "user-123",
            Title = "Title",
            Type = 1,
            CompletionMode = 1,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(context);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            UserId: "user-123",
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
        // Arrange
        await using var context = CreateInMemoryContext();
        var habit = new Habit
        {
            UserId = "user-123",
            Title = "Title",
            Type = 1,
            CompletionMode = 1,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(context);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            UserId: "user-123",
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
        await using var context = CreateInMemoryContext();
        var habit = new Habit
        {
            UserId = "user-123",
            Title = "Title",
            Type = 1,
            CompletionMode = 1,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var handler = new UpdateHabitCommandHandler(context);
        var command = new UpdateHabitCommand(
            Id: habit.Id,
            UserId: "user-123",
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
