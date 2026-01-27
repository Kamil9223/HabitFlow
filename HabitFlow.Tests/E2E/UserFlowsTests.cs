using HabitFlow.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;
using HabitFlow.Tests.IntegrationTests.Infrastructure;
using HabitFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace HabitFlow.Tests.E2E;

[Collection("E2E")]
public sealed class UserFlowsTests : IAsyncLifetime
{
    private readonly E2EFixture _fixture;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private readonly List<string> _pageLogs = new();

    public UserFlowsTests(E2EFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = E2EConfiguration.Headless
        });
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task RegistrationAndLoginWorks()
    {
        var user = TestUser.Create();

        await using var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _fixture.BlazorBaseUrl
        });
        var page = await context.NewPageAsync();
        AttachPageDiagnostics(page);

        await RegisterAsync(page, user);
        await LoginAsync(page, user);

        await page.WaitForURLAsync("**/today");
        await page.GetByText("Brak zaplanowanych nawyków na dzisiaj").WaitForAsync();
    }

    private async Task RegisterAsync(IPage page, TestUser user)
    {
        var response = await page.GotoAsync($"{_fixture.BlazorBaseUrl}/auth/register");
        if (response == null || !response.Ok)
        {
            var status = response?.Status.ToString() ?? "no response";
            throw new InvalidOperationException($"Register page failed to load. Status: {status}");
        }

        await WaitForBlazorInteractiveAsync(page, "register");

        await page.GetByLabel("Email").FillAsync(user.Email);
        await page.GetByLabel("Hasło", new PageGetByLabelOptions { Exact = true }).FillAsync(user.Password);
        await page.GetByLabel("Potwierdź hasło", new PageGetByLabelOptions { Exact = true }).FillAsync(user.Password);
        var submitButton = page.GetByRole(AriaRole.Button, new() { Name = "Zarejestruj się" });
        await submitButton.ClickAsync();

        await WaitForLoginOrErrorAsync(page, "Registration");
        
        // Confirm email automatically
        await ConfirmEmailAsync(user);
    }

    private async Task LoginAsync(IPage page, TestUser user)
    {
        await WaitForBlazorInteractiveAsync(page, "login");
        await page.GetByText("Witaj ponownie!", new() { Exact = true }).WaitForAsync();
        await page.GetByLabel("Email").FillAsync(user.Email);
        await page.GetByLabel("Hasło", new PageGetByLabelOptions { Exact = true }).FillAsync(user.Password);
        
        await page.GetByRole(AriaRole.Button, new() { Name = "Zaloguj się" }).ClickAsync();
        
        // Wait for either success navigation or error
        var todayUrl = page.WaitForURLAsync("**/today", new() { Timeout = 30000 });
        var confirmEmailUrl = page.WaitForURLAsync("**/auth/confirm-email", new() { Timeout = 30000 });
        var errorAlert = page.Locator(".mud-alert-error").WaitForAsync(new() { Timeout = 30000 });
        
        var completed = await Task.WhenAny(todayUrl, confirmEmailUrl, errorAlert);
        
        if (completed == errorAlert)
        {
            var errorText = await page.Locator(".mud-alert-error").InnerTextAsync();
            throw new InvalidOperationException($"Login failed with error: {errorText}");
        }
        
        if (completed == confirmEmailUrl)
        {
            throw new InvalidOperationException("Login redirected to email confirmation instead of /today");
        }
        
        await todayUrl; // Ensure we're on the right page
    }

    private async Task WaitForLoginOrErrorAsync(IPage page, string stage)
    {
        var loginHeading = page.GetByText("Witaj ponownie!", new() { Exact = true });
        var alert = page.Locator(".mud-alert");
        var blazorError = page.Locator("#blazor-error-ui");

        var loginTask = loginHeading.WaitForAsync(new LocatorWaitForOptions { Timeout = 30000 });
        var alertTask = alert.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 30000 });
        var blazorErrorTask = blazorError.WaitForAsync(new LocatorWaitForOptions { Timeout = 30000 });
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));

        try
        {
            var completed = await Task.WhenAny(loginTask, alertTask, blazorErrorTask, timeoutTask);
            if (completed == timeoutTask)
            {
                await ThrowWithDiagnosticsAsync(page, $"{stage} timed out without navigation or error.");
            }

            if (completed == alertTask)
            {
                var message = await alert.First.InnerTextAsync();
                // Success message is also shown as alert - check if it's a success
                if (message.Contains("Konto zostało utworzone", StringComparison.OrdinalIgnoreCase))
                {
                    // This is a success - wait for login page
                    await loginTask;
                    return;
                }
                await ThrowWithDiagnosticsAsync(page, $"{stage} failed: {message}");
            }

            if (completed == blazorErrorTask)
            {
                var message = await blazorError.InnerTextAsync();
                await ThrowWithDiagnosticsAsync(page, $"{stage} failed: {message}");
            }

            await loginTask;
        }
        catch (PlaywrightException)
        {
            if (await alert.CountAsync() > 0)
            {
                var message = await alert.First.InnerTextAsync();
                if (message.Contains("Konto zostało utworzone", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                await ThrowWithDiagnosticsAsync(page, $"{stage} failed: {message}");
            }

            if (await blazorError.IsVisibleAsync())
            {
                var message = await blazorError.InnerTextAsync();
                await ThrowWithDiagnosticsAsync(page, $"{stage} failed: {message}");
            }

            await ThrowWithDiagnosticsAsync(page, $"{stage} failed without visible error.");
        }
    }

    private async Task ThrowWithDiagnosticsAsync(IPage page, string message)
    {
        var artifactsDir = Path.Combine(AppContext.BaseDirectory, "HabitFlow.Tests", "E2E", "artifacts");
        Directory.CreateDirectory(artifactsDir);

        var fileName = $"e2e-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.png";
        var filePath = Path.Combine(artifactsDir, fileName);

        await page.ScreenshotAsync(new PageScreenshotOptions { Path = filePath, FullPage = true });
        var htmlName = Path.ChangeExtension(fileName, ".html");
        var htmlPath = Path.Combine(artifactsDir, htmlName);
        var content = await page.ContentAsync();
        await File.WriteAllTextAsync(htmlPath, content);

        var logName = Path.ChangeExtension(fileName, ".log");
        var logPath = Path.Combine(artifactsDir, logName);
        var blazorError = page.Locator("#blazor-error-ui");
        var blazorErrorText = await blazorError.IsVisibleAsync()
            ? await blazorError.InnerTextAsync()
            : "blazor-error-ui not visible";
        var logLines = new List<string>
        {
            $"url:{page.Url}",
            $"blazor-error-ui:{blazorErrorText}"
        };
        logLines.AddRange(_pageLogs);
        logLines.AddRange(_fixture.ApiOutput.Select(line => $"api:{line}"));
        logLines.AddRange(_fixture.BlazorOutput.Select(line => $"blazor:{line}"));
        await File.WriteAllLinesAsync(logPath, logLines);

        throw new InvalidOperationException($"{message} URL: {page.Url}. Screenshot: {filePath}. HTML: {htmlPath}. Logs: {logPath}");
    }

    private async Task WaitForBlazorInteractiveAsync(IPage page, string stage)
    {
        try
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForFunctionAsync("() => !!window.Blazor");
            await page.WaitForFunctionAsync("() => document.querySelector('body')?.innerText?.length > 0");
        }
        catch (PlaywrightException)
        {
            await ThrowWithDiagnosticsAsync(page, $"Blazor not interactive on {stage} page.");
        }
    }



    private async Task ConfirmEmailAsync(TestUser user)
    {
        // Directly confirm email in database
        var options = new DbContextOptionsBuilder<HabitFlowDbContext>()
            .UseSqlServer(TestDatabase.ConnectionString)
            .Options;

        await using var dbContext = new HabitFlowDbContext(options);
        var appUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        
        if (appUser == null)
        {
            throw new InvalidOperationException($"User {user.Email} not found in database");
        }

        appUser.EmailConfirmed = true;
        await dbContext.SaveChangesAsync();
    }

    private void AttachPageDiagnostics(IPage page)
    {
        page.Console += (_, message) =>
            _pageLogs.Add($"console:{message.Type} {message.Text}");
        page.PageError += (_, exception) =>
            _pageLogs.Add($"pageerror:{exception}");
        page.RequestFailed += (_, request) =>
            _pageLogs.Add($"requestfailed:{request.Url} {request.Failure}");
        page.Request += (_, request) =>
        {
            if (request.Url.Contains("/api/", StringComparison.OrdinalIgnoreCase))
            {
                _pageLogs.Add($"request:{request.Method} {request.Url}");
            }
        };
        page.Response += async (_, response) =>
        {
            if (response.Url.Contains("/api/", StringComparison.OrdinalIgnoreCase))
            {
                var body = "";
                try
                {
                    body = await response.TextAsync();
                }
                catch
                {
                    body = "[could not read body]";
                }
                _pageLogs.Add($"response:{response.Status} {response.Url} body:{body}");
            }
        };
    }

    private sealed record TestUser(string Email, string Password)
    {
        public static TestUser Create()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var randomPart = Guid.NewGuid().ToString("N")[..8];
            var email = $"e2e_{timestamp}_{randomPart}@habitflow.test";
            const string password = "Test1234A";
            return new TestUser(email, password);
        }
    }
}
