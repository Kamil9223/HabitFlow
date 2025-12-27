using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Habits;

/// <summary>
/// Query to retrieve a habit by its ID for a specific user.
/// </summary>
public record GetHabitQuery(
    int HabitId,
    string UserId
) : IQuery<Result<HabitDto>>;

/// <summary>
/// Data transfer object for habit details.
/// </summary>
public record HabitDto(
    int Id,
    string Title,
    string? Description,
    byte Type,
    byte CompletionMode,
    byte DaysOfWeekMask,
    short TargetValue,
    string? TargetUnit,
    DateOnly? DeadlineDate,
    DateTime CreatedAtUtc
);

/// <summary>
/// Handler for retrieving a single habit by ID.
/// Ensures that the habit belongs to the requesting user.
/// </summary>
public class GetHabitQueryHandler(HabitFlowDbContext context)
    : IQueryHandler<GetHabitQuery, Result<HabitDto>>
{
    public async Task<Result<HabitDto>> Handle(GetHabitQuery query, CancellationToken cancellationToken)
    {
        // Validate input
        if (query.HabitId <= 0)
            return Result.Failure<HabitDto>(
                Error.Validation("Habit.InvalidId", "Habit ID must be greater than zero."));

        if (string.IsNullOrWhiteSpace(query.UserId))
            return Result.Failure<HabitDto>(
                Error.Validation("User.InvalidId", "User ID is required."));

        // Query habit with ownership verification
        var habitDto = await context.Habits
            .AsNoTracking()
            .Where(h => h.Id == query.HabitId && h.UserId == query.UserId)
            .Select(h => new HabitDto(
                h.Id,
                h.Title,
                h.Description,
                h.Type,
                h.CompletionMode,
                h.DaysOfWeekMask,
                h.TargetValue,
                h.TargetUnit,
                h.DeadlineDate,
                h.CreatedAtUtc
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (habitDto is null)
            return Result.Failure<HabitDto>(
                Error.NotFound("Habit.NotFound",
                    $"Habit with ID {query.HabitId} was not found or does not belong to the user."));

        return Result.Success(habitDto);
    }
}
