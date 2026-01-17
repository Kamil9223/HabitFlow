using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Core.Features.Progress;

/// <summary>
/// Query to retrieve rolling success rate series for a habit.
/// </summary>
public record GetProgressRollingQuery(
    int HabitId,
    int WindowDays,
    DateOnly? Until
) : IQuery<Result<ProgressRollingResult>>;

/// <summary>
/// Result of rolling success rate query.
/// </summary>
public record ProgressRollingResult(
    int HabitId,
    int WindowDays,
    DateOnly Until,
    IReadOnlyList<ProgressRollingPointDto> Points
);

/// <summary>
/// Data transfer object for a single point in the rolling series.
/// </summary>
public record ProgressRollingPointDto(
    DateOnly Date,
    int PlannedDays,
    double SumDailyScore,
    double SuccessRate
);

/// <summary>
/// Handler for retrieving rolling success rate series.
/// Calculates rolling metrics (planned days, sum daily score, success rate) for each day in the range.
/// </summary>
public class GetProgressRollingQueryHandler(
    HabitFlowDbContext context,
    ILoggedUserContext loggedUserContext,
    ILogger<GetProgressRollingQueryHandler> logger)
    : IQueryHandler<GetProgressRollingQuery, Result<ProgressRollingResult>>
{
    public async Task<Result<ProgressRollingResult>> Handle(
        GetProgressRollingQuery query,
        CancellationToken cancellationToken)
    {
        // Validate query
        var validationErrors = GetProgressRollingValidator.Validate(query);
        if (validationErrors.Count > 0)
            return Result.Failure<ProgressRollingResult>(validationErrors);

        var user = loggedUserContext.GetUser();

        // 1. Verify habit exists and belongs to user
        var habit = await context.Habits
            .AsNoTracking()
            .Where(h => h.Id == query.HabitId && h.UserId == user.UserId)
            .Select(h => new { h.Id, h.DaysOfWeekMask })
            .FirstOrDefaultAsync(cancellationToken);

        if (habit is null)
        {
            logger.LogWarning(
                "User {UserId} attempted to access habit {HabitId} that doesn't exist or doesn't belong to them",
                user.UserId, query.HabitId);

            return Result.Failure<ProgressRollingResult>(
                Error.NotFound("Habit.NotFound", $"Habit with ID {query.HabitId} not found."));
        }

        // 2. Determine date range
        // Use user's timezone from claims for determining "today"
        var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
        var todayInUserTimeZone = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone).Date;
        var until = query.Until ?? DateOnly.FromDateTime(todayInUserTimeZone);
        var startDate = until.AddDays(-query.WindowDays + 1);

        // 3. Fetch all check-ins for the entire range in one query
        var allCheckins = await context.Checkins
            .AsNoTracking()
            .Where(c => c.HabitId == query.HabitId
                     && c.LocalDate >= startDate
                     && c.LocalDate <= until)
            .Select(c => new
            {
                c.LocalDate,
                DailyScore = CalculateDailyScore(
                    c.ActualValue,
                    c.TargetValueSnapshot,
                    c.CompletionModeSnapshot,
                    c.HabitTypeSnapshot),
                c.IsPlanned
            })
            .ToListAsync(cancellationToken);

        // Group check-ins by date for fast lookup
        var checkinsByDate = allCheckins.ToDictionary(c => c.LocalDate);

        // 4. Calculate metrics for each date in the range
        var points = new List<ProgressRollingPointDto>();
        for (var date = startDate; date <= until; date = date.AddDays(1))
        {
            var windowStart = date.AddDays(-query.WindowDays + 1);
            var windowEnd = date;

            // Calculate plannedDays: count days in window that are planned according to DaysOfWeekMask
            var plannedDays = 0;
            for (var d = windowStart; d <= windowEnd; d = d.AddDays(1))
            {
                if (IsDayPlanned(d, habit.DaysOfWeekMask))
                    plannedDays++;
            }

            // Calculate sumDailyScore: sum daily scores from check-ins in window
            var sumDailyScore = allCheckins
                .Where(c => c.LocalDate >= windowStart && c.LocalDate <= windowEnd)
                .Sum(c => c.DailyScore);

            // Calculate successRate
            var successRate = plannedDays > 0 ? sumDailyScore / plannedDays : 0.0;

            points.Add(new ProgressRollingPointDto(
                date,
                plannedDays,
                sumDailyScore,
                successRate));
        }

        logger.LogInformation(
            "Calculated rolling success rate for habit {HabitId}, window {WindowDays}, until {Until}, {PointCount} points",
            query.HabitId, query.WindowDays, until, points.Count);

        return Result.Success(new ProgressRollingResult(
            query.HabitId,
            query.WindowDays,
            until,
            points));
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
    /// Logic copied from GetHabitCalendarQueryHandler for consistency.
    /// </summary>
    private static double CalculateDailyScore(
        int actualValue,
        short targetValueSnapshot,
        Data.Enums.CompletionMode completionModeSnapshot,
        Data.Enums.HabitType habitTypeSnapshot)
    {
        if (targetValueSnapshot <= 0)
            return 0.0;

        double score;

        if (completionModeSnapshot == Data.Enums.CompletionMode.Binary)
        {
            score = actualValue > 0 ? 1.0 : 0.0;
        }
        else
        {
            var ratio = (double)actualValue / targetValueSnapshot;
            var ratioClamped = Math.Clamp(ratio, 0.0, 1.0);

            if (habitTypeSnapshot == Data.Enums.HabitType.Stop)
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
