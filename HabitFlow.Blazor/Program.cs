using HabitFlow.Blazor.Components;
using HabitFlow.Blazor.Services;
using HabitFlow.Client;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Always load static web assets (needed for MudBlazor files from NuGet)
builder.WebHost.UseStaticWebAssets();
StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

var keyRingPath = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", ".authkeys"));

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
    .SetApplicationName("HabitFlow");

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.Name = ".AspNetCore.Identity.Application";
        options.LoginPath = "/auth/login";
        options.AccessDeniedPath = "/auth/login";
    });
builder.Services.AddAuthorization();

// Add authentication state provider
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();

// Add HttpContextAccessor for cookie propagation
builder.Services.AddHttpContextAccessor();

// Configure HttpClient for API with cookie propagation
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl configuration is missing");

builder.Services.AddScoped<System.Net.CookieContainer>();

builder.Services.AddScoped<IHabitFlowApiClient>(sp =>
{
    var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
    var cookieContainer = sp.GetRequiredService<System.Net.CookieContainer>();
    var baseUri = new Uri(apiBaseUrl);

    if (httpContextAccessor?.HttpContext?.Request.Cookies != null)
    {
        foreach (var cookie in httpContextAccessor.HttpContext.Request.Cookies)
        {
            cookieContainer.Add(baseUri, new System.Net.Cookie(cookie.Key, cookie.Value));
        }
    }

    var handler = new HttpClientHandler
    {
        UseCookies = true,
        CookieContainer = cookieContainer
    };

    var httpClient = new HttpClient(handler)
    {
        BaseAddress = baseUri
    };

    return new HabitFlowApiClient(httpClient);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsEnvironment("Testing"))
{
    // In testing environment, show detailed errors for debugging
    app.UseDeveloperExceptionPage();
}
else if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Use traditional static files middleware in Testing for better compatibility
if (app.Environment.IsEnvironment("Testing"))
{
    app.UseStaticFiles();
}
else
{
    app.MapStaticAssets();
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
