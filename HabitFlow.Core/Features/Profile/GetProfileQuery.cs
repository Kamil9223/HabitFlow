using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Abstractions.Services;
using HabitFlow.Core.Common;
using HabitFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Core.Features.Profile;

/// <summary>
/// Query to retrieve current authenticated user profile information.
/// </summary>
public record GetProfileQuery() : IQuery<Result<ProfileDto>>;

/// <summary>
/// Data transfer object for user profile information.
/// </summary>
public record ProfileDto(
    Guid UserId,
    string Email,
    bool EmailConfirmed,
    string TimeZoneId,
    DateTimeOffset CreatedAtUtc,
    int HabitsCount
);

/// <summary>
/// Handler for GetProfileQuery. Retrieves profile data from the database
/// based on the currently authenticated user context.
/// </summary>
public class GetProfileQueryHandler(
    ILoggedUserContext loggedUserContext,
    HabitFlowDbContext dbContext)
    : IQueryHandler<GetProfileQuery, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(
        GetProfileQuery query,
        CancellationToken cancellationToken)
    {
        // Get current user from claims (authenticated via middleware)
        var currentUser = loggedUserContext.GetUser();

        // Fetch user from database with AsNoTracking for read-only optimization
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken);

        // Validate user exists (edge case: user deleted but session still active)
        if (user is null)
        {
            return Result.Failure<ProfileDto>(
                Error.NotFound("User.NotFound", "User account not found."));
        }

        // Get user's habits count
        var habitsCount = await dbContext.Habits
            .AsNoTracking()
            .CountAsync(h => h.UserId == currentUser.UserId, cancellationToken);

        // Map ApplicationUser entity to ProfileDto
        var response = new ProfileDto(
            user.Id,
            user.Email!,
            user.EmailConfirmed,
            user.TimeZoneId,
            new DateTimeOffset(user.CreatedAtUtc, TimeSpan.Zero),
            habitsCount
        );

        return Result.Success(response);
    }
}
