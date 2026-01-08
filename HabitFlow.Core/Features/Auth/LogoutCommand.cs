using HabitFlow.Core.Abstractions;
using HabitFlow.Core.Common;
using HabitFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace HabitFlow.Core.Features.Auth;

public record LogoutCommand() : ICommand<Result>;

public class LogoutCommandHandler(
    SignInManager<ApplicationUser> signInManager)
    : ICommandHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        await signInManager.SignOutAsync();
        return Result.Success();
    }
}
