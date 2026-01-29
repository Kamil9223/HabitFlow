using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using HabitFlow.Data.Entities;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Core.Features.Checkins;

/// <summary>
/// Command to create a daily check-in for a habit.
/// </summary>
public record CreateCheckinCommand(
    int HabitId,
    DateOnly LocalDate,
    int ActualValue
) : ICommand<Result<CreateCheckinResult>>;

/// <summary>
/// Result of creating a check-in.
/// </summary>
public record CreateCheckinResult(
    long Id,
    int HabitId,
    DateOnly LocalDate,
    int ActualValue,
    short TargetValueSnapshot,
    CompletionMode CompletionModeSnapshot,
    HabitType HabitTypeSnapshot,
    bool IsPlanned,
    DateTimeOffset CreatedAtUtc
);

public class CreateCheckinCommandHandler(
    HabitFlowDbContext context,
    ILoggedUserContext loggedUserContext,
    ILogger<CreateCheckinCommandHandler> logger)
    : ICommandHandler<CreateCheckinCommand, Result<CreateCheckinResult>>
{
    public async Task<Result<CreateCheckinResult>> Handle(
        CreateCheckinCommand command,
        CancellationToken cancellationToken)
    {
        // Validate command
        var validationErrors = CreateCheckinCommandValidator.Validate(command);
        if (validationErrors.Count > 0)
            return Result.Failure<CreateCheckinResult>(validationErrors);

        var user = loggedUserContext.GetUser();

        // 1. Fetch habit with required data
        var habit = await context.Habits
            .AsNoTracking()
            .Where(h => h.Id == command.HabitId)
            .Select(h => new
            {
                h.Id,
                h.UserId,
                h.TargetValue,
                h.CompletionMode,
                h.Type,
                h.DaysOfWeekMask
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (habit is null)
        {
            logger.LogWarning("Habit {HabitId} not found", command.HabitId);
            return Result.Failure<CreateCheckinResult>(
                Error.NotFound("Habit.NotFound", $"Habit with ID {command.HabitId} not found"));
        }

        // 2. Check ownership
        if (habit.UserId != user.UserId)
        {
            logger.LogWarning(
                "User {UserId} attempted to create checkin for Habit {HabitId} owned by {OwnerId}",
                user.UserId, command.HabitId, habit.UserId);
            return Result.Failure<CreateCheckinResult>(
                Error.Forbidden("Checkin.Forbidden", "You do not have permission to create checkins for this habit"));
        }

        // 3. Check for duplicate checkin
        var existingCheckin = await context.Checkins
            .AsNoTracking()
            .AnyAsync(c => c.HabitId == command.HabitId && c.LocalDate == command.LocalDate,
                cancellationToken);

        if (existingCheckin)
        {
            logger.LogWarning(
                "Duplicate checkin attempt for Habit {HabitId} on {LocalDate}",
                command.HabitId, command.LocalDate);
            return Result.Failure<CreateCheckinResult>(
                Error.Conflict("Checkin.Duplicate", "A checkin for this date already exists"));
        }

        // 4. Check if day is planned
        // DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6
        // Mask bits: Monday=0, Tuesday=1, ..., Sunday=6
        var bitIndex = command.LocalDate.DayOfWeek == DayOfWeek.Sunday
            ? 6
            : (int)command.LocalDate.DayOfWeek - 1;
        var dayMask = 1 << bitIndex;
        var isPlanned = (habit.DaysOfWeekMask & dayMask) != 0;

        if (!isPlanned)
        {
            logger.LogWarning(
                "Checkin for Habit {HabitId} on {LocalDate} is not allowed (not a planned day)",
                command.HabitId, command.LocalDate);
            return Result.Failure<CreateCheckinResult>(
                Error.UnprocessableEntity("Checkin.NotPlanned", "Checkin is not allowed for this day (not in planned days)"));
        }

        // 5. Create snapshots
        var targetValueSnapshot = habit.TargetValue;

        // 6. Clamp actualValue to targetValueSnapshot
        var clampedActualValue = Math.Min(command.ActualValue, targetValueSnapshot);

        if (clampedActualValue != command.ActualValue)
        {
            logger.LogInformation(
                "ActualValue {Original} clamped to {Clamped} for Habit {HabitId}",
                command.ActualValue, clampedActualValue, command.HabitId);
        }

        // 7. Create checkin entity
        var checkin = new Checkin
        {
            HabitId = command.HabitId,
            UserId = user.UserId,
            LocalDate = command.LocalDate,
            ActualValue = clampedActualValue,
            TargetValueSnapshot = targetValueSnapshot,
            CompletionModeSnapshot = habit.CompletionMode,
            HabitTypeSnapshot = habit.Type,
            IsPlanned = isPlanned,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Checkins.Add(checkin);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Checkin {CheckinId} created for Habit {HabitId} on {LocalDate}",
            checkin.Id, command.HabitId, command.LocalDate);

        // 8. Map to result
        var result = new CreateCheckinResult(
            checkin.Id,
            checkin.HabitId,
            checkin.LocalDate,
            checkin.ActualValue,
            checkin.TargetValueSnapshot,
            habit.CompletionMode,
            habit.Type,
            checkin.IsPlanned,
            checkin.CreatedAtUtc
        );

        return Result.Success(result);
    }
}
