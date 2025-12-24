using System.Text.Json.Serialization;
using HabitFlow.Api.Endpoints;
using HabitFlow.Core;
using HabitFlow.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<HabitFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Core services (Command handlers, dispatchers)
builder.Services.AddCore();

// Configure JSON serialization (camelCase)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Add OpenAPI with NSwag
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "v1";
    config.Title = "HabitFlow API";
    config.Version = "v1";
    config.Description = "REST API for HabitFlow habit tracking application";
});

// Add authorization services (placeholder for JWT later)
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "HabitFlow API";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/v1/swagger.json";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Map API endpoints
app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapHabitEndpoints();
app.MapTodayEndpoints();
app.MapCheckinEndpoints();
app.MapProgressEndpoints();
app.MapNotificationEndpoints();

app.Run();
