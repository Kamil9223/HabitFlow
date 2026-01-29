using HabitFlow.Data;
using HabitFlow.Tests.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HabitFlow.Tests.E2E.Infrastructure;

public class E2EFixture : IAsyncLifetime
{
    private DotnetProcess? _apiProcess;
    private DotnetProcess? _blazorProcess;
    private readonly string _apiBaseUrl;
    private readonly string _blazorBaseUrl;

    public E2EFixture()
    {
        _apiBaseUrl = GetBaseUrl("E2E_API_BASE_URL", 5101);
        _blazorBaseUrl = GetBaseUrl("E2E_BLAZOR_BASE_URL", 5102);
    }

    public string ApiBaseUrl => _apiBaseUrl;
    public string BlazorBaseUrl => _blazorBaseUrl;
    public IReadOnlyCollection<string> ApiOutput => _apiProcess?.RecentOutput ?? Array.Empty<string>();
    public IReadOnlyCollection<string> BlazorOutput => _blazorProcess?.RecentOutput ?? Array.Empty<string>();

    public async Task InitializeAsync()
    {
        try
        {
            await TestDatabase.EnsureStartedAsync();
            await EnsureDatabaseMigratedAsync();

            _apiProcess = DotnetProcess.Start(
                RepositoryPaths.ApiProject,
                ApiBaseUrl,
                BuildApiEnvironment());

            await WaitForServerAsync(ApiBaseUrl, _apiProcess);

            _blazorProcess = DotnetProcess.Start(
                RepositoryPaths.BlazorProject,
                BlazorBaseUrl,
                BuildBlazorEnvironment());

            await WaitForServerAsync(BlazorBaseUrl, _blazorProcess);
            await WaitForBlazorReadyAsync(BlazorBaseUrl, _blazorProcess);
        }
        catch
        {
            if (_blazorProcess != null)
            {
                await _blazorProcess.DisposeAsync();
            }

            if (_apiProcess != null)
            {
                await _apiProcess.DisposeAsync();
            }

            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (_blazorProcess != null)
        {
            await _blazorProcess.DisposeAsync();
        }

        if (_apiProcess != null)
        {
            await _apiProcess.DisposeAsync();
        }
    }

    private static async Task EnsureDatabaseMigratedAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<HabitFlowDbContext>(options =>
            options.UseSqlServer(TestDatabase.ConnectionString));

        using var provider = services.BuildServiceProvider();
        await TestDatabase.EnsureMigratedAsync(provider);
    }

    private Dictionary<string, string> BuildApiEnvironment()
    {
        return new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Testing",
            ["DOTNET_ENVIRONMENT"] = "Testing",
            ["ASPNETCORE_URLS"] = ApiBaseUrl,
            ["ConnectionStrings__DefaultConnection"] = TestDatabase.ConnectionString,
            ["Email__Smtp__Host"] = TestDatabase.SmtpHost,
            ["Email__Smtp__Port"] = TestDatabase.SmtpPort.ToString(),
            ["Email__Smtp__Username"] = "test",
            ["Email__Smtp__Password"] = "test",
            ["Email__FromEmail"] = "test@habitflow.test",
            ["Email__FromName"] = "HabitFlow Test",
            ["Email__Smtp__EnableSsl"] = "false",
            ["NotificationJobSettings__Enabled"] = "false",
            ["LlmSettings__Enabled"] = "false",
            ["Cors__AllowedOrigins__0"] = BlazorBaseUrl
        };
    }

    private Dictionary<string, string> BuildBlazorEnvironment()
    {
        var environment = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Testing",
            ["DOTNET_ENVIRONMENT"] = "Testing",
            ["ASPNETCORE_URLS"] = BlazorBaseUrl,
            ["Api__BaseUrl"] = ApiBaseUrl
        };

        var configuration = GetBuildConfiguration();
        var staticWebAssetsPath = Path.Combine(
            RepositoryPaths.Root,
            "HabitFlow.Blazor",
            "obj",
            configuration,
            "net9.0",
            "staticwebassets.runtime.json");

        if (File.Exists(staticWebAssetsPath))
        {
            environment["ASPNETCORE_STATICWEBASSETS"] = staticWebAssetsPath;
        }

        return environment;
    }

    private static string GetBuildConfiguration()
    {
#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }

    private static string GetBaseUrl(string envKey, int fallbackPort)
    {
        var value = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var port = FindFreePort();
        return $"http://localhost:{port}";
    }

    private static int FindFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitForServerAsync(string baseUrl, DotnetProcess process)
    {
        using var client = new HttpClient();
        var timeoutAt = DateTimeOffset.UtcNow + E2EConfiguration.StartupTimeout;

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            try
            {
                using var _ = await client.GetAsync(baseUrl);
                return;
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(500);
        }

        var output = string.Join(Environment.NewLine, process.RecentOutput);
        throw new TimeoutException($"Server at {baseUrl} did not start in time.{Environment.NewLine}{output}");
    }

    private static async Task WaitForBlazorReadyAsync(string baseUrl, DotnetProcess process)
    {
        using var client = new HttpClient();
        var timeoutAt = DateTimeOffset.UtcNow + E2EConfiguration.StartupTimeout;
        var url = $"{baseUrl}/auth/register";
        string? lastContent = null;
        int? lastStatus = null;

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            try
            {
                using var response = await client.GetAsync(url);
                lastStatus = (int)response.StatusCode;
                lastContent = await response.Content.ReadAsStringAsync();

                if (!string.IsNullOrWhiteSpace(lastContent) &&
                    lastContent.Contains("_framework/blazor.web.js", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(500);
        }

        var output = string.Join(Environment.NewLine, process.RecentOutput);
        throw new TimeoutException(
            $"Blazor content not ready at {url}. Status: {lastStatus}. " +
            $"Content length: {lastContent?.Length ?? 0}.{Environment.NewLine}{output}");
    }
}
