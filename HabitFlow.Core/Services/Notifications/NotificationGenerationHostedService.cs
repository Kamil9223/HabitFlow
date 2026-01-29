using HabitFlow.Core.Abstractions.Notifications;
using HabitFlow.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HabitFlow.Core.Services.Notifications;

/// <summary>
/// Daily background job that triggers miss-due notification generation.
/// Runs at configured time (default: 00:30 UTC daily).
/// </summary>
public sealed class NotificationGenerationHostedService(
    IServiceProvider serviceProvider,
    IOptions<NotificationSettings> settings,
    ILogger<NotificationGenerationHostedService> logger)
    : BackgroundService
{
    private static readonly TimeOnly DefaultRunAtUtc = new(0, 30);
    private readonly NotificationSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Notification generation is disabled.");
            return;
        }

        var runAtUtc = ResolveRunAtUtc(_settings.CronSchedule);
        logger.LogInformation("Notification job scheduled to run daily at {RunAt} UTC", runAtUtc);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNextRun(DateTime.UtcNow, runAtUtc);
            logger.LogInformation("Next notification job run in {Delay}", delay);

            try
            {
                //await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Notification job cancelled during delay.");
                return;
            }

            if (stoppingToken.IsCancellationRequested)
                return;

            await RunJobAsync(stoppingToken);
            break;
        }
    }

    private async Task RunJobAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Notification generation job started.");

        using var scope = serviceProvider.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<INotificationGenerationService>();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(Math.Max(1, _settings.MaxExecutionMinutes)));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);

        try
        {
            var summary = await generator.GenerateNotificationsAsync(linkedCts.Token);
            var errorRate = summary.HabitsProcessed == 0
                ? 0
                : (double)summary.Errors / summary.HabitsProcessed;

            if (errorRate > 0.1)
            {
                logger.LogWarning(
                    "Notification generation completed with high error rate. Processed={Processed}, Created={Created}, Errors={Errors}",
                    summary.HabitsProcessed,
                    summary.NotificationsCreated,
                    summary.Errors);
            }
            else
            {
                logger.LogInformation(
                    "Notification generation completed. Processed={Processed}, Created={Created}, Errors={Errors}",
                    summary.HabitsProcessed,
                    summary.NotificationsCreated,
                    summary.Errors);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
        {
            logger.LogError("Notification generation job timed out.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notification generation job failed.");
        }
    }

    private TimeOnly ResolveRunAtUtc(string? cronSchedule)
    {
        if (string.IsNullOrWhiteSpace(cronSchedule))
            return DefaultRunAtUtc;

        var parts = cronSchedule.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            logger.LogWarning("Invalid CronSchedule format. Using default 00:30 UTC.");
            return DefaultRunAtUtc;
        }

        if (!int.TryParse(parts[1], out var minute) || !int.TryParse(parts[2], out var hour))
        {
            logger.LogWarning("Invalid CronSchedule values. Using default 00:30 UTC.");
            return DefaultRunAtUtc;
        }

        if (hour is < 0 or > 23 || minute is < 0 or > 59)
        {
            logger.LogWarning("CronSchedule values out of range. Using default 00:30 UTC.");
            return DefaultRunAtUtc;
        }

        return new TimeOnly(hour, minute);
    }

    private static TimeSpan CalculateDelayUntilNextRun(DateTime utcNow, TimeOnly runAtUtc)
    {
        var nextRun = utcNow.Date
            .AddHours(runAtUtc.Hour)
            .AddMinutes(runAtUtc.Minute);

        // If time has passed today, schedule for tomorrow
        if (nextRun <= utcNow)
            nextRun = nextRun.AddDays(1);

        return nextRun - utcNow;
    }
}
