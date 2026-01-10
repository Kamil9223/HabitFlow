using HabitFlow.Core.Services;

namespace HabitFlow.Core.Abstractions.Services;

public interface ILoggedUserContext
{
    CurrentUser GetUser();
}