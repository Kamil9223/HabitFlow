using HabitFlow.Core.Abstractions.Notifications;
using HabitFlow.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HabitFlow.Core.Services.Notifications;

/// <summary>
/// Daily background job that triggers miss-due notification generation.
/// </summary>
public sealed class NotificationGenerationHostedService(
    IServiceProvider serviceProvider,
    IOptions<NotificationJobSettings> jobOptions,
    IOptions<NotificationFeaturesOptions> featureOptions,
    ILogger<NotificationGenerationHostedService> logger)
    : BackgroundService
{
    private static readonly TimeOnly DefaultRunAtUtc = new(0, 30);
    private readonly NotificationJobSettings _jobSettings = jobOptions.Value;
    private readonly NotificationFeaturesOptions _features = featureOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_jobSettings.Enabled || !_features.NotificationsEnabled)
        {
            logger.LogInformation("Notification generation hosted service is disabled.");
            return;
        }

        var runAtUtc = ResolveRunAtUtc(_jobSettings, logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelay(DateTime.UtcNow, runAtUtc);
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            await RunJobAsync(stoppingToken);
        }
    }

    private async Task RunJobAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Notification generation job started.");

        using var scope = serviceProvider.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<INotificationGenerationService>();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(Math.Max(1, _jobSettings.MaxExecutionMinutes)));
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

    private static TimeOnly ResolveRunAtUtc(NotificationJobSettings settings, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(settings.CronSchedule))
            return DefaultRunAtUtc;

        var parts = settings.CronSchedule.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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

    private static TimeSpan CalculateDelay(DateTime utcNow, TimeOnly runAtUtc)
    {
        var nextRun = new DateTime(
            utcNow.Year,
            utcNow.Month,
            utcNow.Day,
            runAtUtc.Hour,
            runAtUtc.Minute,
            0,
            DateTimeKind.Utc);

        if (nextRun <= utcNow)
            nextRun = nextRun.AddDays(1);

        return nextRun - utcNow;
    }
}
