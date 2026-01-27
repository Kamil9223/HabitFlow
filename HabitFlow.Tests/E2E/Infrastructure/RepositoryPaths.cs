namespace HabitFlow.Tests.E2E.Infrastructure;

internal static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();

    public static string ApiProject =>
        Path.Combine(Root, "HabitFlow.Api", "HabitFlow.Api.csproj");

    public static string BlazorProject =>
        Path.Combine(Root, "HabitFlow.Blazor", "HabitFlow.Blazor.csproj");

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HabitFlow.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Cannot locate repository root (HabitFlow.sln).");
    }
}
