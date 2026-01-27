namespace HabitFlow.Tests.E2E.Infrastructure;

internal static class E2EConfiguration
{
    public static string ApiBaseUrl =>
        GetValueOrDefault("E2E_API_BASE_URL", "http://localhost:5101");

    public static string BlazorBaseUrl =>
        GetValueOrDefault("E2E_BLAZOR_BASE_URL", "http://localhost:5102");

    public static bool Headless =>
        !string.Equals(Environment.GetEnvironmentVariable("E2E_HEADFUL"), "1", StringComparison.OrdinalIgnoreCase);

    public static TimeSpan StartupTimeout =>
        TimeSpan.FromSeconds(GetIntOrDefault("E2E_STARTUP_TIMEOUT_SECONDS", 60));

    private static string GetValueOrDefault(string key, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static int GetIntOrDefault(string key, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return int.TryParse(value, out var result) ? result : fallback;
    }
}
