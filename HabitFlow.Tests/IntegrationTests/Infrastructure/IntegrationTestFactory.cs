using HabitFlow.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace HabitFlow.Tests.IntegrationTests.Infrastructure;

public sealed class IntegrationTestFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    private readonly string _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["Email:Smtp:Host"] = "localhost",
                ["Email:Smtp:Port"] = "2525",
                ["Email:Smtp:Username"] = "test",
                ["Email:Smtp:Password"] = "test",
                ["Email:FromEmail"] = "test@habitflow.test",
                ["Email:FromName"] = "HabitFlow Test",
                ["Email:Smtp:EnableSsl"] = "false"
            });
        });
    }
}
