using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using HabitFlow.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Habits;

/// <summary>
/// Supported fields for sorting habits.
/// </summary>
public enum HabitSortField
{
    CreatedAtUtc,
    Title,
    DeadlineDate
}

/// <summary>
/// Query to retrieve a paginated list of habits for a specific user with optional filters and sorting.
/// </summary>
public record GetHabitsQuery(
    string UserId,
    int Page = 1,
    int PageSize = 20,
    HabitType? Type = null,
    CompletionMode? CompletionMode = null,
    bool? Active = true,
    string? Search = null,
    HabitSortField SortField = HabitSortField.CreatedAtUtc,
    SortDirection SortDirection = SortDirection.Desc
) : IQuery<Result<PagedHabitsDto>>;

/// <summary>
/// Data transfer object for paginated habits list.
/// </summary>
public record PagedHabitsDto(
    int TotalCount,
    IReadOnlyList<HabitDto> Items
);

/// <summary>
/// Handler for retrieving a paginated, filtered, and sorted list of habits.
/// Applies filters for type, completionMode, active status, and text search.
/// Supports sorting by CreatedAtUtc, Title, and DeadlineDate.
/// </summary>
public class GetHabitsQueryHandler(HabitFlowDbContext context)
    : IQueryHandler<GetHabitsQuery, Result<PagedHabitsDto>>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;
    private const int MinPage = 1;

    public async Task<Result<PagedHabitsDto>> Handle(GetHabitsQuery query, CancellationToken cancellationToken)
    {
        // Validate user ID
        if (string.IsNullOrWhiteSpace(query.UserId))
            return Result.Failure<PagedHabitsDto>(
                Error.Validation("User.InvalidId", "User ID is required."));

        // Validate page and pageSize
        var pageSize = Math.Clamp(query.PageSize, MinPageSize, MaxPageSize);
        var page = Math.Max(query.Page, MinPage);

        // Build base query with user filter
        var habitsQuery = context.Habits
            .AsNoTracking()
            .Where(h => h.UserId == query.UserId);

        // Apply optional filters
        if (query.Type.HasValue)
            habitsQuery = habitsQuery.Where(h => h.Type == query.Type.Value);

        if (query.CompletionMode.HasValue)
            habitsQuery = habitsQuery.Where(h => h.CompletionMode == query.CompletionMode.Value);

        if (query.Active.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (query.Active.Value)
                // Active: deadline is null or >= today
                habitsQuery = habitsQuery.Where(h => h.DeadlineDate == null || h.DeadlineDate >= today);
            else
                // Inactive: deadline < today
                habitsQuery = habitsQuery.Where(h => h.DeadlineDate != null && h.DeadlineDate < today);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
            habitsQuery = habitsQuery.Where(h => h.Title.Contains(query.Search));

        // Get total count before pagination
        var totalCount = await habitsQuery.CountAsync(cancellationToken);

        // Apply sorting
        habitsQuery = ApplySort(habitsQuery, query.SortField, query.SortDirection);

        // Apply pagination
        var habits = await habitsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedHabitsDto(totalCount, habits));
    }

    private static IQueryable<Data.Entities.Habit> ApplySort(
        IQueryable<Data.Entities.Habit> query,
        HabitSortField field,
        SortDirection direction)
    {
        return (field, direction) switch
        {
            (HabitSortField.CreatedAtUtc, SortDirection.Asc) => query.OrderBy(h => h.CreatedAtUtc),
            (HabitSortField.CreatedAtUtc, SortDirection.Desc) => query.OrderByDescending(h => h.CreatedAtUtc),
            (HabitSortField.Title, SortDirection.Asc) => query.OrderBy(h => h.Title),
            (HabitSortField.Title, SortDirection.Desc) => query.OrderByDescending(h => h.Title),
            (HabitSortField.DeadlineDate, SortDirection.Asc) => query.OrderBy(h => h.DeadlineDate),
            (HabitSortField.DeadlineDate, SortDirection.Desc) => query.OrderByDescending(h => h.DeadlineDate),
            _ => query.OrderByDescending(h => h.CreatedAtUtc)
        };
    }
}
