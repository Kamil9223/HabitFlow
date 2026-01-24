using HabitFlow.Core.Abstractions.Notifications;
using HabitFlow.Core.Options;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HabitFlow.Core.Services.Notifications;

/// <summary>
/// Orchestrates miss-due notification detection and creation.
/// </summary>
public sealed class NotificationGenerationService(
    HabitFlowDbContext context,
    INotificationRepository notificationRepository,
    INotificationContentGenerator contentGenerator,
    IOptions<NotificationJobSettings> jobOptions,
    IOptions<NotificationFeaturesOptions> featureOptions,
    IOptions<LlmSettings> llmOptions,
    FallbackContentGenerator fallbackGenerator,
    ILogger<NotificationGenerationService> logger) : INotificationGenerationService
{
    private const int CompletionRateLookbackDays = 30;
    private const int StreakLookbackDays = 90;

    private readonly NotificationJobSettings _jobSettings = jobOptions.Value;
    private readonly NotificationFeaturesOptions _features = featureOptions.Value;
    private readonly LlmSettings _llmSettings = llmOptions.Value;
    private readonly FallbackContentGenerator _fallbackGenerator = fallbackGenerator;

    public async Task<NotificationGenerationSummary> GenerateNotificationsAsync(CancellationToken cancellationToken)
    {
        if (!_features.NotificationsEnabled)
        {
            logger.LogInformation("Notification generation is disabled via feature flags.");
            return new NotificationGenerationSummary(0, 0, 0);
        }

        var batchSize = Math.Max(1, _jobSettings.BatchSize);
        var habitsProcessed = 0;
        var notificationsCreated = 0;
        var errors = 0;
        var aiCallsUsed = 0;
        var aiBudgetEnabled = _llmSettings.Enabled
            && _features.AiNotifications.Enabled
            && !_features.AiNotifications.FallbackOnly;
        var aiMaxCalls = Math.Max(0, _llmSettings.MaxDailyRequests);
        var aiBudgetAvailable = aiBudgetEnabled && aiMaxCalls > 0;
        var aiForceFallback = aiBudgetEnabled && aiMaxCalls == 0;

        var usersQuery = context.Users
            .AsNoTracking()
            .Select(u => new UserSnapshot(u.Id, u.TimeZoneId));

        var batchIndex = 0;
        while (true)
        {
            var users = await usersQuery
                .Skip(batchIndex * batchSize)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (users.Count == 0)
                break;

            foreach (var user in users)
            {
                if (!TryResolveLocalYesterday(user.TimeZoneId, out var localYesterday, out var timeZoneInfo))
                {
                    errors++;
                    logger.LogWarning("Skipping user {UserId} due to invalid timezone.", user.UserId);
                    continue;
                }

                var habits = await context.Habits
                    .AsNoTracking()
                    .Where(h => h.UserId == user.UserId)
                    .Select(h => new HabitSnapshot(
                        h.Id,
                        h.Title,
                        h.DaysOfWeekMask,
                        h.CreatedAtUtc,
                        h.DeadlineDate))
                    .ToListAsync(cancellationToken);

                if (habits.Count == 0)
                    continue;

                var plannedHabits = habits
                    .Where(h => IsPlannedDay(localYesterday, h.DaysOfWeekMask))
                    .Where(h => h.DeadlineDate is null || localYesterday <= h.DeadlineDate.Value)
                    .ToList();

                if (plannedHabits.Count == 0)
                    continue;

                var completedHabitIds = await context.Checkins
                    .AsNoTracking()
                    .Where(c => c.UserId == user.UserId && c.LocalDate == localYesterday)
                    .Select(c => c.HabitId)
                    .ToListAsync(cancellationToken);

                var pendingHabits = plannedHabits
                    .Where(h => !completedHabitIds.Contains(h.Id))
                    .ToList();

                if (pendingHabits.Count == 0)
                    continue;

                var pendingHabitIds = pendingHabits.Select(h => h.Id).ToList();
                var completionStats = await context.Checkins
                    .AsNoTracking()
                    .Where(c => c.UserId == user.UserId && pendingHabitIds.Contains(c.HabitId))
                    .GroupBy(c => c.HabitId)
                    .Select(g => new CompletionStats(
                        g.Key,
                        g.Count(),
                        g.Max(c => c.LocalDate)))
                    .ToListAsync(cancellationToken);

                var completionStatsByHabit = completionStats.ToDictionary(s => s.HabitId);
                var lookbackStart = localYesterday.AddDays(-CompletionRateLookbackDays + 1);
                var streakLookbackStart = localYesterday.AddDays(-StreakLookbackDays + 1);

                var recentCheckins = await context.Checkins
                    .AsNoTracking()
                    .Where(c => c.UserId == user.UserId)
                    .Where(c => pendingHabitIds.Contains(c.HabitId))
                    .Where(c => c.LocalDate >= streakLookbackStart && c.LocalDate <= localYesterday)
                    .Select(c => new { c.HabitId, c.LocalDate })
                    .ToListAsync(cancellationToken);

                var recentCheckinsByHabit = recentCheckins
                    .GroupBy(c => c.HabitId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.LocalDate).ToHashSet());

                foreach (var habit in pendingHabits)
                {
                    habitsProcessed++;

                    if (await notificationRepository.ExistsAsync(
                        user.UserId,
                        habit.Id,
                        localYesterday,
                        NotificationType.MissDue,
                        cancellationToken))
                    {
                        continue;
                    }

                    var habitCreatedLocal = DateOnly.FromDateTime(
                        TimeZoneInfo.ConvertTimeFromUtc(habit.CreatedAtUtc, timeZoneInfo));
                    if (localYesterday < habitCreatedLocal)
                        continue;

                    var stats = completionStatsByHabit.GetValueOrDefault(habit.Id);
                    var totalCompletions = stats?.TotalCompletions ?? 0;
                    var daysSinceLast = stats?.LastCompletionDate is null
                        ? 0
                        : Math.Max(1, localYesterday.DayNumber - stats.LastCompletionDate.DayNumber);

                    var completionRate = CalculateCompletionRate(
                        habit,
                        recentCheckinsByHabit.GetValueOrDefault(habit.Id),
                        lookbackStart,
                        localYesterday,
                        habitCreatedLocal);

                    var streakDays = CalculateStreakDays(
                        habit,
                        recentCheckinsByHabit.GetValueOrDefault(habit.Id),
                        localYesterday.AddDays(-1),
                        habitCreatedLocal);

                    var contentContext = new NotificationContentContext(
                        user.UserId,
                        habit.Id,
                        habit.Title,
                        streakDays,
                        totalCompletions,
                        daysSinceLast,
                        completionRate);

                    NotificationContentResult contentResult;
                    try
                    {
                        if (aiForceFallback || (aiBudgetAvailable && aiCallsUsed >= aiMaxCalls))
                        {
                            var fallback = await _fallbackGenerator.GenerateAsync(contentContext, cancellationToken);
                            contentResult = fallback with
                            {
                                AiError = TrimError("Limit AI na dzien zostal osiagniety - uzyto szablonu.")
                            };
                        }
                        else
                        {
                            contentResult = await contentGenerator.GenerateAsync(contentContext, cancellationToken);
                            if (contentResult.Status == AiGenerationStatus.Success)
                                aiCallsUsed++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        logger.LogError(ex, "Notification content generation failed for habit {HabitId}.", habit.Id);
                        contentResult = new NotificationContentResult(
                            "Wczoraj nie udalo sie zrobic nawyku. Wrocmy na dobre tory!",
                            AiGenerationStatus.Error,
                            TrimError(ex.Message));
                    }

                    contentResult = EnsureSafeContent(contentResult);

                    var notification = new Notification
                    {
                        UserId = user.UserId,
                        HabitId = habit.Id,
                        LocalDate = localYesterday,
                        Type = NotificationType.MissDue,
                        Content = contentResult.Content,
                        AiStatus = contentResult.Status,
                        AiError = TrimError(contentResult.AiError),
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    try
                    {
                        await notificationRepository.CreateAsync(notification, cancellationToken);
                        notificationsCreated++;
                    }
                    catch (DbUpdateException ex)
                    {
                        errors++;
                        logger.LogWarning(ex, "Failed to create notification for habit {HabitId}.", habit.Id);
                    }
                }
            }

            batchIndex++;
        }

        return new NotificationGenerationSummary(habitsProcessed, notificationsCreated, errors);
    }

    private static bool TryResolveLocalYesterday(
        string timeZoneId,
        out DateOnly localYesterday,
        out TimeZoneInfo timeZoneInfo)
    {
        localYesterday = default;
        timeZoneInfo = TimeZoneInfo.Utc;

        if (string.IsNullOrWhiteSpace(timeZoneId))
            return false;

        try
        {
            timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var localNow = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZoneInfo);
            localYesterday = DateOnly.FromDateTime(localNow.Date.AddDays(-1));
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static bool IsPlannedDay(DateOnly date, byte daysOfWeekMask)
    {
        var bitIndex = date.DayOfWeek == DayOfWeek.Sunday
            ? 6
            : (int)date.DayOfWeek - 1;

        var mask = (byte)(1 << bitIndex);
        return (daysOfWeekMask & mask) != 0;
    }

    private static double CalculateCompletionRate(
        HabitSnapshot habit,
        HashSet<DateOnly>? checkins,
        DateOnly startDate,
        DateOnly endDate,
        DateOnly habitStartDate)
    {
        var planned = 0;
        var completed = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date < habitStartDate)
                continue;

            if (!IsPlannedDay(date, habit.DaysOfWeekMask))
                continue;

            planned++;
            if (checkins?.Contains(date) == true)
                completed++;
        }

        return planned == 0 ? 0 : (double)completed / planned;
    }

    private static int CalculateStreakDays(
        HabitSnapshot habit,
        HashSet<DateOnly>? checkins,
        DateOnly startDate,
        DateOnly habitStartDate)
    {
        if (checkins is null || checkins.Count == 0)
            return 0;

        var streak = 0;
        for (var date = startDate; date >= habitStartDate; date = date.AddDays(-1))
        {
            if (!IsPlannedDay(date, habit.DaysOfWeekMask))
                continue;

            if (checkins.Contains(date))
            {
                streak++;
                continue;
            }

            break;
        }

        return streak;
    }

    private static string? TrimError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;

        return message.Length <= 512 ? message : message[..512];
    }

    private static NotificationContentResult EnsureSafeContent(NotificationContentResult result)
    {
        if (IsContentSafe(result.Content))
            return result;

        return result with
        {
            Content = "Wczoraj nie udalo sie zrobic nawyku. Wrocmy na dobre tory!",
            Status = AiGenerationStatus.Error,
            AiError = TrimError("Zablokowana lub pusta tresc.")
        };
    }

    private static bool IsContentSafe(string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 1024)
            return false;

        var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < 3)
            return false;

        var lower = content.ToLowerInvariant();
        foreach (var blocked in BlockedPhrases)
        {
            if (lower.Contains(blocked, StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static readonly string[] BlockedPhrases =
    [
        "nienawidze",
        "zabij",
        "samoboj",
        "glup"
    ];

    private sealed record UserSnapshot(Guid UserId, string TimeZoneId);

    private sealed record HabitSnapshot(
        int Id,
        string Title,
        byte DaysOfWeekMask,
        DateTime CreatedAtUtc,
        DateOnly? DeadlineDate);

    private sealed record CompletionStats(
        int HabitId,
        int TotalCompletions,
        DateOnly LastCompletionDate);
}
