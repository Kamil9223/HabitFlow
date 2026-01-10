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

public class GetHabitCalendarQueryHandlerTests
{
    private readonly HabitFlowDbContext _dbContext;
    private readonly ILoggedUserContext _userContext;
    
    private readonly Guid _userId = Guid.NewGuid();
    
    public GetHabitCalendarQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new HabitFlowDbContext(options);
        _userContext = NSubstitute.Substitute.For<ILoggedUserContext>();
        _userContext.GetUser().Returns(x => new CurrentUser(_userId, "UTC", "user-123@test.pl"));
    }

    [Fact]
    public async Task Handle_ValidQueryWithCheckins_ReturnsSuccessWithCalendarData()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Read books",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127, // Every day
            TargetValue = 10,
            TargetUnit = "pages",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var checkin = new Checkin
        {
            HabitId = habit.Id,
            UserId = _userId,
            LocalDate = new DateOnly(2025, 12, 15),
            ActualValue = 7,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Checkins.Add(checkin);
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitCalendarQueryHandler(_dbContext, _userContext);
        var query = new GetHabitCalendarQuery(
            habit.Id,
            new DateOnly(2025, 12, 15),
            new DateOnly(2025, 12, 15));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(habit.Id, result.Value.HabitId);
        Assert.Single(result.Value.Days);

        var day = result.Value.Days[0];
        Assert.Equal(new DateOnly(2025, 12, 15), day.Date);
        Assert.True(day.IsPlanned);
        Assert.Equal(7, day.ActualValue);
        Assert.Equal(10, day.TargetValueSnapshot!.Value);
        Assert.Equal(CompletionMode.Quantitative, day.CompletionModeSnapshot);
        Assert.Equal(HabitType.Start, day.HabitTypeSnapshot);
        Assert.Equal(0.7, day.DailyScore, precision: 2);
    }

    [Fact]
    public async Task Handle_DateRangeWithoutCheckins_ReturnsEmptyDays()
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

        var handler = new GetHabitCalendarQueryHandler(_dbContext, _userContext);
        var query = new GetHabitCalendarQuery(
            habit.Id,
            new DateOnly(2025, 12, 1),
            new DateOnly(2025, 12, 3));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.Days.Count);

        foreach (var day in result.Value.Days)
        {
            Assert.True(day.IsPlanned);
            Assert.Equal(0, day.ActualValue);
            Assert.Null(day.TargetValueSnapshot);
            Assert.Null(day.CompletionModeSnapshot);
            Assert.Null(day.HabitTypeSnapshot);
            Assert.Equal(0.0, day.DailyScore);
        }
    }

    [Fact]
    public async Task Handle_HabitWithSelectiveDays_CorrectlyMarksPlannedDays()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Weekday habit",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 31, // Mon-Fri (bits 0-4)
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitCalendarQueryHandler(_dbContext, _userContext);
        // Using a known date range: Jan 6-12, 2025
        // Jan 6, 2025 is Monday
        var query = new GetHabitCalendarQuery(
            habit.Id,
            new DateOnly(2025, 1, 5), // Sunday
            new DateOnly(2025, 1, 11)); // Saturday

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.Days.Count);

        // Verify each day
        Assert.Equal(DayOfWeek.Sunday, result.Value.Days[0].Date.DayOfWeek);
        Assert.False(result.Value.Days[0].IsPlanned); // Sunday - not planned

        Assert.Equal(DayOfWeek.Monday, result.Value.Days[1].Date.DayOfWeek);
        Assert.True(result.Value.Days[1].IsPlanned); // Monday - planned

        Assert.Equal(DayOfWeek.Tuesday, result.Value.Days[2].Date.DayOfWeek);
        Assert.True(result.Value.Days[2].IsPlanned); // Tuesday - planned

        Assert.Equal(DayOfWeek.Wednesday, result.Value.Days[3].Date.DayOfWeek);
        Assert.True(result.Value.Days[3].IsPlanned); // Wednesday - planned

        Assert.Equal(DayOfWeek.Thursday, result.Value.Days[4].Date.DayOfWeek);
        Assert.True(result.Value.Days[4].IsPlanned); // Thursday - planned

        Assert.Equal(DayOfWeek.Friday, result.Value.Days[5].Date.DayOfWeek);
        Assert.True(result.Value.Days[5].IsPlanned); // Friday - planned

        Assert.Equal(DayOfWeek.Saturday, result.Value.Days[6].Date.DayOfWeek);
        Assert.False(result.Value.Days[6].IsPlanned); // Saturday - not planned
    }

    [Fact]
    public async Task Handle_BinaryCompletionMode_CalculatesCorrectScore()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Binary habit",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var checkin1 = new Checkin
        {
            HabitId = habit.Id,
            UserId = _userId,
            LocalDate = new DateOnly(2025, 12, 1),
            ActualValue = 1,
            TargetValueSnapshot = 1,
            CompletionModeSnapshot = CompletionMode.Binary,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var checkin2 = new Checkin
        {
            HabitId = habit.Id,
            UserId = _userId,
            LocalDate = new DateOnly(2025, 12, 2),
            ActualValue = 0,
            TargetValueSnapshot = 1,
            CompletionModeSnapshot = CompletionMode.Binary,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Checkins.AddRange(checkin1, checkin2);
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitCalendarQueryHandler(_dbContext, _userContext);
        var query = new GetHabitCalendarQuery(
            habit.Id,
            new DateOnly(2025, 12, 1),
            new DateOnly(2025, 12, 2));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Days.Count);
        Assert.Equal(1.0, result.Value.Days[0].DailyScore);
        Assert.Equal(0.0, result.Value.Days[1].DailyScore);
    }

    [Fact]
    public async Task Handle_StopHabit_CalculatesInvertedScore()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Stop habit",
            Type = HabitType.Stop,
            CompletionMode = CompletionMode.Quantitative, // Quantitative
            DaysOfWeekMask = 127,
            TargetValue = 3, // Max 3 violations
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var checkin1 = new Checkin
        {
            HabitId = habit.Id,
            UserId = _userId,
            LocalDate = new DateOnly(2025, 12, 1),
            ActualValue = 0, // Perfect (no violations)
            TargetValueSnapshot = 3,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Stop,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var checkin2 = new Checkin
        {
            HabitId = habit.Id,
            UserId = _userId,
            LocalDate = new DateOnly(2025, 12, 2),
            ActualValue = 3, // Max violations
            TargetValueSnapshot = 3,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Stop,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var checkin3 = new Checkin
        {
            HabitId = habit.Id,
            UserId = _userId,
            LocalDate = new DateOnly(2025, 12, 3),
            ActualValue = 1, // 1 violation out of 3
            TargetValueSnapshot = 3,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Stop,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Checkins.AddRange(checkin1, checkin2, checkin3);
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitCalendarQueryHandler(_dbContext, _userContext);
        var query = new GetHabitCalendarQuery(
            habit.Id,
            new DateOnly(2025, 12, 1),
            new DateOnly(2025, 12, 3));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Days.Count);
        Assert.Equal(1.0, result.Value.Days[0].DailyScore, precision: 2); // 1 - 0/3 = 1.0
        Assert.Equal(0.0, result.Value.Days[1].DailyScore, precision: 2); // 1 - 3/3 = 0.0
        Assert.Equal(0.67, result.Value.Days[2].DailyScore, precision: 2); // 1 - 1/3 ≈ 0.67
    }

    [Fact]
    public async Task Handle_HabitNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var handler = new GetHabitCalendarQueryHandler(_dbContext, _userContext);
        var query = new GetHabitCalendarQuery(
            999,
            new DateOnly(2025, 12, 1),
            new DateOnly(2025, 12, 31));

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
            Title = "Test habit",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitCalendarQueryHandler(_dbContext, _userContext);
        var query = new GetHabitCalendarQuery(
            habit.Id,
            new DateOnly(2025, 12, 1),
            new DateOnly(2025, 12, 31));

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
        var handler = new GetHabitCalendarQueryHandler(_dbContext, _userContext);
        var query = new GetHabitCalendarQuery(
            invalidId,
            new DateOnly(2025, 12, 1),
            new DateOnly(2025, 12, 31));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Habit.InvalidId", result.Error.Code);
    }

    [Fact]
    public async Task Handle_FromDateAfterToDate_ReturnsValidationError()
    {
        // Arrange
        var handler = new GetHabitCalendarQueryHandler(_dbContext, _userContext);
        var query = new GetHabitCalendarQuery(
            1,
            new DateOnly(2025, 12, 31),
            new DateOnly(2025, 12, 1));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("DateRange.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task Handle_DateRangeExceeds90Days_ReturnsValidationError()
    {
        // Arrange ;
        var handler = new GetHabitCalendarQueryHandler(_dbContext, _userContext);
        var query = new GetHabitCalendarQuery(
            1,
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 4, 5)); // 95 days

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("DateRange.TooLarge", result.Error.Code);
        Assert.Contains("90 days", result.Error.Description);
    }

    [Fact]
    public async Task Handle_Exactly90DaysRange_ReturnsSuccess()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Test habit",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitCalendarQueryHandler(_dbContext, _userContext);
        var query = new GetHabitCalendarQuery(
            habit.Id,
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 3, 31)); // Exactly 90 days

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(90, result.Value.Days.Count);
    }

    [Fact]
    public async Task Handle_MixedCheckinsAndEmptyDays_ReturnsCorrectCalendar()
    {
        // Arrange
        var habit = new Habit
        {
            UserId = _userId,
            Title = "Mixed habit",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = 127,
            TargetValue = 10,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        // Add check-ins for days 1 and 3, but not day 2
        var checkin1 = new Checkin
        {
            HabitId = habit.Id,
            UserId = _userId,
            LocalDate = new DateOnly(2025, 12, 1),
            ActualValue = 8,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var checkin3 = new Checkin
        {
            HabitId = habit.Id,
            UserId = _userId,
            LocalDate = new DateOnly(2025, 12, 3),
            ActualValue = 5,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Checkins.AddRange(checkin1, checkin3);
        await _dbContext.SaveChangesAsync();

        var handler = new GetHabitCalendarQueryHandler(_dbContext, _userContext);
        var query = new GetHabitCalendarQuery(
            habit.Id,
            new DateOnly(2025, 12, 1),
            new DateOnly(2025, 12, 3));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Days.Count);

        // Day 1 - has check-in
        Assert.Equal(8, result.Value.Days[0].ActualValue);
        Assert.Equal(10, result.Value.Days[0].TargetValueSnapshot!.Value);
        Assert.Equal(0.8, result.Value.Days[0].DailyScore, precision: 2);

        // Day 2 - no check-in
        Assert.Equal(0, result.Value.Days[1].ActualValue);
        Assert.Null(result.Value.Days[1].TargetValueSnapshot);
        Assert.Equal(0.0, result.Value.Days[1].DailyScore);

        // Day 3 - has check-in
        Assert.Equal(5, result.Value.Days[2].ActualValue);
        Assert.Equal(10, result.Value.Days[2].TargetValueSnapshot!.Value);
        Assert.Equal(0.5, result.Value.Days[2].DailyScore, precision: 2);
    }
}
