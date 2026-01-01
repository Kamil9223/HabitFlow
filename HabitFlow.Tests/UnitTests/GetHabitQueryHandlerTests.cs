using HabitFlow.Core.Features.Habits;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class GetHabitQueryHandlerTests
{
    private HabitFlowDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HabitFlowDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidQueryForExistingHabit_ReturnsSuccessWithHabitDto()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";
        var habit = new Habit
        {
            UserId = userId,
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
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var handler = new GetHabitQueryHandler(context);
        var query = new GetHabitQuery(habit.Id, userId);

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
        await using var context = CreateInMemoryContext();
        var handler = new GetHabitQueryHandler(context);
        var query = new GetHabitQuery(999, "user-123");

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
        await using var context = CreateInMemoryContext();
        var habit = new Habit
        {
            UserId = "user-123",
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var handler = new GetHabitQueryHandler(context);
        var query = new GetHabitQuery(habit.Id, "different-user");

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
        await using var context = CreateInMemoryContext();
        var handler = new GetHabitQueryHandler(context);
        var query = new GetHabitQuery(invalidId, "user-123");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.InvalidId", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_InvalidUserId_ReturnsValidationError(string? invalidUserId)
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var handler = new GetHabitQueryHandler(context);
        var query = new GetHabitQuery(1, invalidUserId!);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidId", result.Error.Code);
    }

    [Fact]
    public async Task Handle_HabitWithMinimalFields_ReturnsSuccessWithNullOptionalFields()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";
        var habit = new Habit
        {
            UserId = userId,
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
        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        var handler = new GetHabitQueryHandler(context);
        var query = new GetHabitQuery(habit.Id, userId);

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
        // Arrange
        await using var context = CreateInMemoryContext();
        var userId = "user-123";

        var habit1 = new Habit
        {
            UserId = userId,
            Title = "Habit 1",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        var habit2 = new Habit
        {
            UserId = userId,
            Title = "Habit 2",
            Type = HabitType.Stop,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 63,
            TargetValue = 5,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.AddRange(habit1, habit2);
        await context.SaveChangesAsync();

        var handler = new GetHabitQueryHandler(context);
        var query = new GetHabitQuery(habit2.Id, userId);

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
