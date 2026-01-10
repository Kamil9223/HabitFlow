using System.Security.Claims;
using HabitFlow.Core.Abstractions.Services;
using Microsoft.AspNetCore.Http;

namespace HabitFlow.Core.Services;

public sealed record CurrentUser(Guid UserId, string TimeZoneId, string Email)
{
    public static CurrentUser Anonymous => new(Guid.Empty, "UTC", string.Empty);
}

public sealed class LoggedUserContext(IHttpContextAccessor httpContextAccessor) : ILoggedUserContext
{
    public CurrentUser GetUser()
    {
        var principal = httpContextAccessor.HttpContext.User;
        if (principal is null)
            return CurrentUser.Anonymous;

        var id = principal.FindFirst(claim => claim.Type == ClaimTypes.NameIdentifier)!.Value;

        return new CurrentUser(
            Guid.Parse(id),
            principal.FindFirst(claim => claim.Type == "TimeZoneId")!.Value,
            principal.FindFirst(claim => claim.Type == ClaimTypes.Email)!.Value);
    }
}