using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Today;

/// <summary>
/// Query to retrieve today's planned items for a user, optionally for a specific local date.
/// </summary>
public record GetTodayQuery(
    string UserId,
    DateOnly? Date
) : IQuery<Result<TodayDto>>;

/// <summary>
/// Data transfer object for the Today view.
/// </summary>
public record TodayDto(
    DateOnly Date,
    IReadOnlyList<TodayItemDto> Items
);

/// <summary>
/// Data transfer object for a single planned item in Today view.
/// </summary>
public record TodayItemDto(
    int HabitId,
    string Title,
    HabitType Type,
    CompletionMode CompletionMode,
    short TargetValue,
    string? TargetUnit,
    bool IsPlanned,
    bool HasCheckin
);

/// <summary>
/// Handler for retrieving today's planned items with check-in status.
/// </summary>
public class GetTodayQueryHandler(HabitFlowDbContext context)
    : IQueryHandler<GetTodayQuery, Result<TodayDto>>
{
    public async Task<Result<TodayDto>> Handle(GetTodayQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
            return Result.Failure<TodayDto>(
                Error.Validation("User.InvalidId", "User ID is required."));

        var targetDateResult = await ResolveTargetDate(query, cancellationToken);
        if (targetDateResult.IsFailure)
            return Result.Failure<TodayDto>(targetDateResult.Errors);

        var targetDate = targetDateResult.Value;
        var dayMask = GetDayMask(targetDate);

        var items = await context.Habits
            .AsNoTracking()
            .Where(h => h.UserId == query.UserId)
            .Where(h => (h.DaysOfWeekMask & dayMask) != 0)
            .Select(h => new TodayItemDto(
                h.Id,
                h.Title,
                h.Type,
                h.CompletionMode,
                h.TargetValue,
                h.TargetUnit,
                true,
                context.Checkins.Any(c =>
                    c.HabitId == h.Id &&
                    c.UserId == query.UserId &&
                    c.LocalDate == targetDate)
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(new TodayDto(targetDate, items));
    }

    private async Task<Result<DateOnly>> ResolveTargetDate(GetTodayQuery query, CancellationToken cancellationToken)
    {
        if (query.Date.HasValue)
            return Result.Success(query.Date.Value);

        var timeZoneId = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == query.UserId)
            .Select(u => u.TimeZoneId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(timeZoneId))
            return Result.Failure<DateOnly>(
                Error.Validation("User.TimeZoneMissing", "User time zone is required."));

        try
        {
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var localNow = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZoneInfo);
            return Result.Success(DateOnly.FromDateTime(localNow));
        }
        catch (TimeZoneNotFoundException)
        {
            return Result.Failure<DateOnly>(
                Error.Validation("User.InvalidTimeZone", "User time zone is invalid."));
        }
        catch (InvalidTimeZoneException)
        {
            return Result.Failure<DateOnly>(
                Error.Validation("User.InvalidTimeZone", "User time zone is invalid."));
        }
    }

    private static byte GetDayMask(DateOnly date)
    {
        // DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6
        // Mask bits: Monday=0, ..., Sunday=6
        var bitIndex = date.DayOfWeek == DayOfWeek.Sunday
            ? 6
            : (int)date.DayOfWeek - 1;

        return (byte)(1 << bitIndex);
    }
}
