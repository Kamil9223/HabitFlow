using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Core.Features.Checkins;

/// <summary>
/// Query to retrieve check-ins for a habit within a date range.
/// </summary>
public record GetCheckinsQuery(
    int HabitId,
    DateOnly From,
    DateOnly To
) : IQuery<Result<GetCheckinsResult>>;

/// <summary>
/// Result of retrieving check-ins for a habit.
/// </summary>
public record GetCheckinsResult(
    int HabitId,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<CheckinDetailDto> Items
);

/// <summary>
/// Data transfer object for a detailed check-in item with snapshots.
/// </summary>
public record CheckinDetailDto(
    long Id,
    DateOnly LocalDate,
    int ActualValue,
    short TargetValueSnapshot,
    byte CompletionModeSnapshot,
    byte HabitTypeSnapshot,
    bool IsPlanned
);

public class GetCheckinsQueryHandler(
    HabitFlowDbContext context,
    ILoggedUserContext loggedUserContext,
    ILogger<GetCheckinsQueryHandler> logger)
    : IQueryHandler<GetCheckinsQuery, Result<GetCheckinsResult>>
{
    public async Task<Result<GetCheckinsResult>> Handle(
        GetCheckinsQuery query,
        CancellationToken cancellationToken)
    {
        // Validate query
        var validationErrors = GetCheckinsQueryValidator.Validate(query);
        if (validationErrors.Count > 0)
            return Result.Failure<GetCheckinsResult>(validationErrors);

        var user = loggedUserContext.GetUser();

        // 1. Verify habit exists and belongs to user (ownership check)
        var habitExists = await context.Habits
            .AsNoTracking()
            .AnyAsync(h => h.Id == query.HabitId && h.UserId == user.UserId,
                cancellationToken);

        if (!habitExists)
        {
            logger.LogWarning(
                "User {UserId} attempted to access habit {HabitId} that doesn't exist or doesn't belong to them",
                user.UserId, query.HabitId);

            return Result.Failure<GetCheckinsResult>(
                Error.NotFound("Habit.NotFound", $"Habit with ID {query.HabitId} was not found"));
        }

        // 2. Retrieve check-ins within date range
        // Uses covering index IX_Checkins_HabitId_LocalDate with INCLUDE columns
        var checkins = await context.Checkins
            .AsNoTracking()
            .Where(c => c.HabitId == query.HabitId
                     && c.LocalDate >= query.From
                     && c.LocalDate <= query.To)
            .OrderBy(c => c.LocalDate)
            .Select(c => new CheckinDetailDto(
                c.Id,
                c.LocalDate,
                c.ActualValue,
                c.TargetValueSnapshot,
                (byte)c.CompletionModeSnapshot,
                (byte)c.HabitTypeSnapshot,
                c.IsPlanned
            ))
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Retrieved {Count} checkins for habit {HabitId} from {From} to {To}",
            checkins.Count, query.HabitId, query.From, query.To);

        var result = new GetCheckinsResult(
            query.HabitId,
            query.From,
            query.To,
            checkins
        );

        return Result.Success(result);
    }
}
