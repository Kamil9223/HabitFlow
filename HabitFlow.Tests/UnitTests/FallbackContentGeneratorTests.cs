using HabitFlow.Core.Abstractions.Notifications;
using HabitFlow.Core.Services.Notifications;
using HabitFlow.Data.Enums;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class FallbackContentGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_UsesTemplateAndReplacesPlaceholders()
    {
        var generator = new FallbackContentGenerator(new Random(1));
        var context = new NotificationContentContext(
            Guid.NewGuid(),
            123,
            "Morning Run",
            5,
            12,
            1,
            0.75);

        var result = await generator.GenerateAsync(context, CancellationToken.None);

        Assert.Equal(AiGenerationStatus.Fallback, result.Status);
        Assert.Contains("Morning Run", result.Content);
        Assert.DoesNotContain("{habitName}", result.Content);
        Assert.DoesNotContain("{streakDays}", result.Content);
        Assert.DoesNotContain("{totalCompletions}", result.Content);
        Assert.False(string.IsNullOrWhiteSpace(result.AiError));
    }

    [Fact]
    public async Task GenerateAsync_EmptyHabitName_UsesFallbackLabel()
    {
        var generator = new FallbackContentGenerator(new Random(2));
        var context = new NotificationContentContext(
            Guid.NewGuid(),
            456,
            "",
            0,
            0,
            0,
            0.1);

        var result = await generator.GenerateAsync(context, CancellationToken.None);

        Assert.Equal(AiGenerationStatus.Fallback, result.Status);
        Assert.Contains("twoj nawyk", result.Content);
    }
}
