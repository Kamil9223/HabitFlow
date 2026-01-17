using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Checkins;

/// <summary>
/// Query to retrieve all check-ins for a specific date across all user's habits.
/// </summary>
public record GetCheckinsByDateQuery(
    DateOnly Date
) : IQuery<Result<IReadOnlyList<CheckinItemDto>>>;

/// <summary>
/// Data transfer object for a single check-in item.
/// </summary>
public record CheckinItemDto(
    long Id,
    int HabitId,
    DateOnly LocalDate,
    int ActualValue,
    bool IsPlanned
);

/// <summary>
/// Handler for retrieving check-ins by date.
/// </summary>
public class GetCheckinsByDateQueryHandler(
    HabitFlowDbContext context,
    ILoggedUserContext loggedUserContext)
    : IQueryHandler<GetCheckinsByDateQuery, Result<IReadOnlyList<CheckinItemDto>>>
{
    public async Task<Result<IReadOnlyList<CheckinItemDto>>> Handle(
        GetCheckinsByDateQuery query,
        CancellationToken cancellationToken)
    {
        var loggedUser = loggedUserContext.GetUser();

        // Query check-ins for the specified date
        var items = await context.Checkins
            .AsNoTracking()
            .Where(c => c.UserId == loggedUser.UserId && c.LocalDate == query.Date)
            .OrderBy(c => c.HabitId)
            .Select(c => new CheckinItemDto(
                c.Id,
                c.HabitId,
                c.LocalDate,
                c.ActualValue,
                c.IsPlanned
            ))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CheckinItemDto>>(items);
    }
}
