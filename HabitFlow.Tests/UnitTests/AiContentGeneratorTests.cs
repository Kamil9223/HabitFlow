using HabitFlow.Core.Abstractions.Notifications;
using HabitFlow.Core.Common;
using HabitFlow.Core.Options;
using HabitFlow.Core.Services.Notifications;
using HabitFlow.Data.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace HabitFlow.Tests.UnitTests;

public class AiContentGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_LlmSuccess_ReturnsSuccessContent()
    {
        var llmClient = Substitute.For<ILlmClient>();
        llmClient.GenerateCompletionAsync(Arg.Any<LlmCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("Keep going today."));

        var generator = CreateGenerator(llmClient);
        var context = BuildContext();

        var result = await generator.GenerateAsync(context, CancellationToken.None);

        Assert.Equal(AiGenerationStatus.Success, result.Status);
        Assert.Equal("Keep going today.", result.Content);
        Assert.Null(result.AiError);
    }

    [Fact]
    public async Task GenerateAsync_LlmFailure_FallsBack()
    {
        var llmClient = Substitute.For<ILlmClient>();
        llmClient.GenerateCompletionAsync(Arg.Any<LlmCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(Error.Failure("Llm.Error", "LLM error")));

        var generator = CreateGenerator(llmClient);
        var context = BuildContext();

        var result = await generator.GenerateAsync(context, CancellationToken.None);

        Assert.Equal(AiGenerationStatus.Fallback, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.Content));
        Assert.False(string.IsNullOrWhiteSpace(result.AiError));
    }

    private static AiContentGenerator CreateGenerator(ILlmClient llmClient)
    {
        var llmSettings = Options.Create(new LlmSettings
        {
            Enabled = true,
            Provider = "OpenAI",
            MaxRetries = 0
        });
        var features = Options.Create(new NotificationFeaturesOptions
        {
            NotificationsEnabled = true,
            AiNotifications = new NotificationFeaturesOptions.AiNotificationsOptions
            {
                Enabled = true,
                FallbackOnly = false
            }
        });

        return new AiContentGenerator(
            llmClient,
            new FallbackContentGenerator(new Random(3)),
            llmSettings,
            features,
            NullLogger<AiContentGenerator>.Instance);
    }

    private static NotificationContentContext BuildContext()
        => new(
            Guid.NewGuid(),
            10,
            "Stretching",
            3,
            12,
            1,
            0.6);
}
