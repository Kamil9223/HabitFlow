using HabitFlow.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                ["Email:Smtp:Host"] = TestDatabase.SmtpHost,
                ["Email:Smtp:Port"] = TestDatabase.SmtpPort.ToString(),
                ["Email:Smtp:Username"] = "test",
                ["Email:Smtp:Password"] = "test",
                ["Email:FromEmail"] = "test@habitflow.test",
                ["Email:FromName"] = "HabitFlow Test",
                ["Email:Smtp:EnableSsl"] = "false",
                ["NotificationJobSettings:Enabled"] = "false",
                ["LlmSettings:Enabled"] = "false",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft"] = "Warning",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
            });
        });
    }
}
