using HabitFlow.Blazor.Components;
using HabitFlow.Blazor.Services;
using HabitFlow.Client;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add authentication state provider
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();

// Add HttpContextAccessor for cookie propagation
builder.Services.AddHttpContextAccessor();

// Configure HttpClient for API with cookie propagation
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Api:BaseUrl configuration is missing");

builder.Services.AddHttpClient<IHabitFlowApiClient, HabitFlowApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
    var handler = new HttpClientHandler
    {
        UseCookies = true,
        CookieContainer = new System.Net.CookieContainer()
    };

    // Propagate cookies from HTTP context to API calls
    if (httpContextAccessor?.HttpContext?.Request.Cookies != null)
    {
        var baseUri = new Uri(apiBaseUrl);
        foreach (var cookie in httpContextAccessor.HttpContext.Request.Cookies)
        {
            handler.CookieContainer.Add(baseUri, new System.Net.Cookie(cookie.Key, cookie.Value));
        }
    }

    return handler;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();