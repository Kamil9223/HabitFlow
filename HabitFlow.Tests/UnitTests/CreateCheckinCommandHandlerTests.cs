using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Features.Checkins;
using HabitFlow.Core.Services;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class CreateCheckinCommandHandlerTests
{
    private readonly HabitFlowDbContext _dbContext;
    private readonly ILoggedUserContext _userContext;
    private readonly ILogger<CreateCheckinCommandHandler> _logger;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    public CreateCheckinCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HabitFlowDbContext(options);
        _userContext = Substitute.For<ILoggedUserContext>();
        _userContext.GetUser().Returns(x => new CurrentUser(_userId, "UTC", "user@test.pl"));
        _logger = Substitute.For<ILogger<CreateCheckinCommandHandler>>();
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithCheckinData()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127, // All days
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateCheckinCommandHandler(_dbContext, _userContext, _logger);
        var command = new CreateCheckinCommand(
            HabitId: habit.Id,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: 7);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Id > 0);
        Assert.Equal(habit.Id, result.Value.HabitId);
        Assert.Equal(7, result.Value.ActualValue);
        Assert.Equal(10, result.Value.TargetValueSnapshot);
        Assert.Equal(CompletionMode.Quantitative, result.Value.CompletionModeSnapshot);
        Assert.Equal(HabitType.Start, result.Value.HabitTypeSnapshot);
        Assert.True(result.Value.IsPlanned);

        var checkin = await _dbContext.Checkins.FirstOrDefaultAsync(c => c.Id == result.Value.Id);
        Assert.NotNull(checkin);
        Assert.Equal(_userId, checkin.UserId);
    }

    [Fact]
    public async Task Handle_HabitNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var handler = new CreateCheckinCommandHandler(_dbContext, _userContext, _logger);
        var command = new CreateCheckinCommand(
            HabitId: 999,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: 5);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbiddenError()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _otherUserId, // Different user
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateCheckinCommandHandler(_dbContext, _userContext, _logger);
        var command = new CreateCheckinCommand(
            HabitId: habit.Id,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: 5);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Checkin.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task Handle_DuplicateCheckin_ReturnsConflictError()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var localDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var existingCheckin = new Checkin
        {
            HabitId = habit.Id,
            UserId = _userId,
            LocalDate = localDate,
            ActualValue = 5,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Checkins.Add(existingCheckin);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateCheckinCommandHandler(_dbContext, _userContext, _logger);
        var command = new CreateCheckinCommand(
            HabitId: habit.Id,
            LocalDate: localDate,
            ActualValue: 7);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Checkin.Duplicate", result.Error.Code);
    }

    [Fact]
    public async Task Handle_NotPlannedDay_ReturnsValidationError()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dayOfWeek = (int)today.DayOfWeek;
        var allDaysExceptToday = (byte)(127 & ~(1 << dayOfWeek)); // All days except today

        var habit = new Habit
        {
            UserId = _userId,
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = allDaysExceptToday,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateCheckinCommandHandler(_dbContext, _userContext, _logger);
        var command = new CreateCheckinCommand(
            HabitId: habit.Id,
            LocalDate: today,
            ActualValue: 5);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Checkin.NotPlanned", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ActualValueExceedsTarget_ClampsValue()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateCheckinCommandHandler(_dbContext, _userContext, _logger);
        var command = new CreateCheckinCommand(
            HabitId: habit.Id,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: 15); // Exceeds target of 10

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.ActualValue); // Clamped to target
        Assert.Equal(10, result.Value.TargetValueSnapshot);
    }

    [Fact]
    public async Task Handle_BinaryMode_AcceptsZeroOrOne()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Exercise",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateCheckinCommandHandler(_dbContext, _userContext, _logger);
        var command = new CreateCheckinCommand(
            HabitId: habit.Id,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: 1);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.ActualValue);
        Assert.Equal(CompletionMode.Binary, result.Value.CompletionModeSnapshot);
    }

    [Fact]
    public async Task Handle_NegativeActualValue_ReturnsValidationError()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateCheckinCommandHandler(_dbContext, _userContext, _logger);
        var command = new CreateCheckinCommand(
            HabitId: habit.Id,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: -5);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("ActualValue", result.Errors.Select(e => e.Code));
    }

    [Fact]
    public async Task Handle_FutureDate_ReturnsValidationError()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateCheckinCommandHandler(_dbContext, _userContext, _logger);
        var command = new CreateCheckinCommand(
            HabitId: habit.Id,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            ActualValue: 5);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("LocalDate", result.Errors.Select(e => e.Code));
    }

    [Fact]
    public async Task Handle_DateMoreThan7DaysBack_ReturnsValidationError()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateCheckinCommandHandler(_dbContext, _userContext, _logger);
        var command = new CreateCheckinCommand(
            HabitId: habit.Id,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-8)),
            ActualValue: 5);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("LocalDate", result.Errors.Select(e => e.Code));
    }

    [Fact]
    public async Task Handle_StopHabit_CreatesCheckinWithCorrectType()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Quit smoking",
            Type = HabitType.Stop,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127,
            TargetValue = 0, // Target violations
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new CreateCheckinCommandHandler(_dbContext, _userContext, _logger);
        var command = new CreateCheckinCommand(
            HabitId: habit.Id,
            LocalDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ActualValue: 0);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HabitType.Stop, result.Value.HabitTypeSnapshot);
        Assert.Equal(0, result.Value.ActualValue);
    }
}
