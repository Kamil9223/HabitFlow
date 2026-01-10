using System.Security.Claims;
using HabitFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HabitFlow.Core.Infrastructure;

/// <summary>
/// Custom factory that adds TimeZoneId to user claims.
/// </summary>
public class CustomUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser>(userManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Add TimeZoneId claim if available
        if (!string.IsNullOrWhiteSpace(user.TimeZoneId))
        {
            identity.AddClaim(new Claim("TimeZoneId", user.TimeZoneId));
        }

        return identity;
    }
}
