using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Habits;

/// <summary>
/// Query to retrieve calendar data for a habit within a date range.
/// Returns daily statuses including check-in data and computed scores.
/// </summary>
public record GetHabitCalendarQuery(
    int HabitId,
    string UserId,
    DateOnly From,
    DateOnly To
) : IQuery<Result<HabitCalendarDto>>;

/// <summary>
/// Data transfer object for habit calendar.
/// </summary>
public record HabitCalendarDto(
    int HabitId,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<CalendarDayDto> Days
);

/// <summary>
/// Data transfer object for a single day in the calendar.
/// </summary>
public record CalendarDayDto(
    DateOnly Date,
    bool IsPlanned,
    int ActualValue,
    int? TargetValueSnapshot,
    byte? CompletionModeSnapshot,
    byte? HabitTypeSnapshot,
    double DailyScore
);

/// <summary>
/// Handler for retrieving habit calendar data.
/// Combines habit schedule information with check-in data to produce daily statuses.
/// </summary>
public class GetHabitCalendarQueryHandler(HabitFlowDbContext context)
    : IQueryHandler<GetHabitCalendarQuery, Result<HabitCalendarDto>>
{
    private const int MaxRangeDays = 90;

    public async Task<Result<HabitCalendarDto>> Handle(
        GetHabitCalendarQuery query,
        CancellationToken cancellationToken)
    {
        // Validate input
        if (query.HabitId <= 0)
            return Result.Failure<HabitCalendarDto>(
                Error.Validation("Habit.InvalidId", "Habit ID must be greater than zero."));

        if (string.IsNullOrWhiteSpace(query.UserId))
            return Result.Failure<HabitCalendarDto>(
                Error.Validation("User.InvalidId", "User ID is required."));

        if (query.From > query.To)
            return Result.Failure<HabitCalendarDto>(
                Error.Validation("DateRange.Invalid", "From date must be before or equal to To date."));

        var rangeDays = query.To.DayNumber - query.From.DayNumber + 1;
        if (rangeDays > MaxRangeDays)
            return Result.Failure<HabitCalendarDto>(
                Error.Validation("DateRange.TooLarge",
                    $"Date range cannot exceed {MaxRangeDays} days. Requested: {rangeDays} days."));

        // Verify habit exists and belongs to user
        var habit = await context.Habits
            .AsNoTracking()
            .Where(h => h.Id == query.HabitId && h.UserId == query.UserId)
            .Select(h => new
            {
                h.Id,
                h.DaysOfWeekMask,
                h.Type,
                h.CompletionMode,
                h.TargetValue
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (habit is null)
            return Result.Failure<HabitCalendarDto>(
                Error.NotFound("Habit.NotFound",
                    $"Habit with ID {query.HabitId} was not found or does not belong to the user."));

        // Fetch all check-ins within date range
        var checkins = await context.Checkins
            .AsNoTracking()
            .Where(c => c.HabitId == query.HabitId
                        && c.LocalDate >= query.From
                        && c.LocalDate <= query.To)
            .Select(c => new
            {
                c.LocalDate,
                c.ActualValue,
                c.TargetValueSnapshot,
                c.CompletionModeSnapshot,
                c.HabitTypeSnapshot,
                c.IsPlanned
            })
            .ToDictionaryAsync(c => c.LocalDate, cancellationToken);

        // Generate calendar days
        var days = new List<CalendarDayDto>();
        for (var date = query.From; date <= query.To; date = date.AddDays(1))
        {
            var isPlanned = IsDayPlanned(date, habit.DaysOfWeekMask);

            if (checkins.TryGetValue(date, out var checkin))
            {
                // Day has a check-in
                var dailyScore = CalculateDailyScore(
                    checkin.ActualValue,
                    checkin.TargetValueSnapshot,
                    checkin.CompletionModeSnapshot,
                    checkin.HabitTypeSnapshot);

                days.Add(new CalendarDayDto(
                    date,
                    checkin.IsPlanned,
                    checkin.ActualValue,
                    checkin.TargetValueSnapshot,
                    checkin.CompletionModeSnapshot,
                    checkin.HabitTypeSnapshot,
                    dailyScore));
            }
            else
            {
                // Day without check-in
                days.Add(new CalendarDayDto(
                    date,
                    isPlanned,
                    ActualValue: 0,
                    TargetValueSnapshot: null,
                    CompletionModeSnapshot: null,
                    HabitTypeSnapshot: null,
                    DailyScore: 0.0));
            }
        }

        return Result.Success(new HabitCalendarDto(
            query.HabitId,
            query.From,
            query.To,
            days));
    }

    /// <summary>
    /// Determines if a day is planned based on the days of week mask.
    /// Mask bits: 0=Monday, 1=Tuesday, ..., 6=Sunday (1-127)
    /// </summary>
    private static bool IsDayPlanned(DateOnly date, byte daysOfWeekMask)
    {
        // DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6
        // Convert to 0-based mask: Monday=0, ..., Sunday=6
        var dayOfWeek = date.DayOfWeek;
        var bitIndex = dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1;

        return (daysOfWeekMask & (1 << bitIndex)) != 0;
    }

    /// <summary>
    /// Calculates daily score based on actual value, target, completion mode, and habit type.
    /// </summary>
    private static double CalculateDailyScore(
        int actualValue,
        short targetValueSnapshot,
        byte completionModeSnapshot,
        byte habitTypeSnapshot)
    {
        if (targetValueSnapshot <= 0)
            return 0.0;

        double score;

        // CompletionMode: 1=Binary, 2=Quantitative, 3=Checklist
        if (completionModeSnapshot == 1) // Binary
        {
            score = actualValue > 0 ? 1.0 : 0.0;
        }
        else // Quantitative (2) or Checklist (3)
        {
            var ratio = (double)actualValue / targetValueSnapshot;
            var ratioClamped = Math.Clamp(ratio, 0.0, 1.0);

            // HabitType: 1=Start, 2=Stop
            if (habitTypeSnapshot == 2) // Stop
            {
                // For Stop habits, lower actual value is better
                score = 1.0 - ratioClamped;
            }
            else // Start (1) or default
            {
                score = ratioClamped;
            }
        }

        return score;
    }
}
