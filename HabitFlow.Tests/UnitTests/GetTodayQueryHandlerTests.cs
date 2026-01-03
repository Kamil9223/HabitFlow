using HabitFlow.Core.Features.Today;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class GetTodayQueryHandlerTests
{
    private static HabitFlowDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HabitFlowDbContext(options);
    }

    [Fact]
    public async Task Handle_WithDate_ReturnsPlannedItemsAndCheckinStatus()
    {
        await using var context = CreateInMemoryContext();
        var userId = "user-123";
        var date = new DateOnly(2025, 12, 7); // Sunday
        var plannedMask = GetDayMask(date);

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        var plannedHabit = new Habit
        {
            UserId = userId,
            Title = "Read",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Quantitative,
            DaysOfWeekMask = plannedMask,
            TargetValue = 10,
            TargetUnit = "pages",
            CreatedAtUtc = DateTime.UtcNow
        };
        var unplannedHabit = new Habit
        {
            UserId = userId,
            Title = "Unplanned",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 1, // Monday
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Habits.AddRange(plannedHabit, unplannedHabit);
        await context.SaveChangesAsync();

        context.Checkins.Add(new Checkin
        {
            HabitId = plannedHabit.Id,
            UserId = userId,
            LocalDate = date,
            ActualValue = 7,
            TargetValueSnapshot = 10,
            CompletionModeSnapshot = CompletionMode.Quantitative,
            HabitTypeSnapshot = HabitType.Start,
            IsPlanned = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new GetTodayQueryHandler(context);
        var result = await handler.Handle(new GetTodayQuery(userId, date), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(plannedHabit.Id, result.Value.Items[0].HabitId);
        Assert.True(result.Value.Items[0].HasCheckin);
    }

    [Fact]
    public async Task Handle_WithoutDate_UsesUserTimeZone()
    {
        await using var context = CreateInMemoryContext();
        var userId = "user-123";

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow
        });

        context.Habits.Add(new Habit
        {
            UserId = userId,
            Title = "Daily habit",
            Type = HabitType.Start,
            CompletionMode = CompletionMode.Binary,
            DaysOfWeekMask = 127,
            TargetValue = 1,
            CreatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var before = DateOnly.FromDateTime(DateTime.UtcNow);
        var handler = new GetTodayQueryHandler(context);
        var result = await handler.Handle(new GetTodayQuery(userId, null), CancellationToken.None);
        var after = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Date == before || result.Value.Date == after);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_InvalidUserId_ReturnsValidationError(string? invalidUserId)
    {
        await using var context = CreateInMemoryContext();
        var handler = new GetTodayQueryHandler(context);

        var result = await handler.Handle(new GetTodayQuery(invalidUserId!, new DateOnly(2025, 12, 7)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidId", result.Error.Code);
    }

    [Fact]
    public async Task Handle_MissingTimeZone_ReturnsValidationError()
    {
        await using var context = CreateInMemoryContext();
        var userId = "user-123";

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "",
            CreatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new GetTodayQueryHandler(context);
        var result = await handler.Handle(new GetTodayQuery(userId, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.TimeZoneMissing", result.Error.Code);
    }

    [Fact]
    public async Task Handle_InvalidTimeZone_ReturnsValidationError()
    {
        await using var context = CreateInMemoryContext();
        var userId = "user-123";

        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            TimeZoneId = "Invalid/Zone",
            CreatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var handler = new GetTodayQueryHandler(context);
        var result = await handler.Handle(new GetTodayQuery(userId, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidTimeZone", result.Error.Code);
    }

    private static byte GetDayMask(DateOnly date)
    {
        var bitIndex = date.DayOfWeek == DayOfWeek.Sunday
            ? 6
            : (int)date.DayOfWeek - 1;

        return (byte)(1 << bitIndex);
    }
}
