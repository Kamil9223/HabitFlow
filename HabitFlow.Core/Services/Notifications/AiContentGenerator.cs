using HabitFlow.Core.Abstractions.Notifications;
using HabitFlow.Core.Options;
using HabitFlow.Data.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HabitFlow.Core.Services.Notifications;

/// <summary>
/// AI-first notification generator with fallback support.
/// </summary>
public sealed class AiContentGenerator(
    ILlmClient llmClient,
    FallbackContentGenerator fallbackGenerator,
    IOptions<LlmSettings> llmOptions,
    IOptions<NotificationFeaturesOptions> featureOptions,
    ILogger<AiContentGenerator> logger) : INotificationContentGenerator
{
    private readonly LlmSettings _llmSettings = llmOptions.Value;
    private readonly NotificationFeaturesOptions _features = featureOptions.Value;

    public async Task<NotificationContentResult> GenerateAsync(
        NotificationContentContext context,
        CancellationToken cancellationToken)
    {
        if (!_features.AiNotifications.Enabled || _features.AiNotifications.FallbackOnly || !_llmSettings.Enabled)
            return await GenerateFallbackAsync(context, "AI wylaczone - uzyto szablonu.", cancellationToken);

        if (!string.Equals(_llmSettings.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
            return await GenerateFallbackAsync(context, "Nieobslugiwany dostawca LLM - uzyto szablonu.", cancellationToken);

        var request = BuildRequest(context);
        var response = await llmClient.GenerateCompletionAsync(request, cancellationToken);
        if (response.IsFailure)
        {
            logger.LogWarning("LLM generation failed with code {Code}.", response.Error.Code);
            return await GenerateFallbackAsync(context, response.Error.Description, cancellationToken);
        }

        if (!IsContentValid(response.Value))
            return await GenerateFallbackAsync(context, "Odpowiedz LLM nie przeszla walidacji - uzyto szablonu.", cancellationToken);

        return new NotificationContentResult(response.Value, AiGenerationStatus.Success, null);
    }

    private LlmCompletionRequest BuildRequest(NotificationContentContext context)
    {
        var systemPrompt = "You are a supportive habit coach.";

        var lastCompletion = context.TotalCompletions == 0
            ? "never"
            : $"{context.DaysSinceLastCompletion} days ago";

        var userPrompt = $"""
User Context:
- Habit: "{context.HabitName}"
- Streak before miss: {context.StreakDays} days
- Total completions: {context.TotalCompletions}
- Last completed: {lastCompletion}

Task: Generate a SHORT (max 100 words), empathetic, motivational message
that acknowledges the missed day without guilt-tripping, and encourages
the user to get back on track. Use a warm, personal tone.

Rules:
- Do NOT use emojis
- Do NOT use user's name (we don't have it)
- Focus on the habit's value, not the failure
- End with actionable encouragement
- Return the notification in Polish
""";

        return new LlmCompletionRequest(
            systemPrompt,
            userPrompt,
            Math.Max(1, _llmSettings.MaxTokens),
            _llmSettings.Temperature,
            TimeSpan.FromSeconds(Math.Max(1, _llmSettings.TimeoutSeconds)));
    }

    private async Task<NotificationContentResult> GenerateFallbackAsync(
        NotificationContentContext context,
        string aiError,
        CancellationToken cancellationToken)
    {
        var fallback = await fallbackGenerator.GenerateAsync(context, cancellationToken);
        var trimmedError = TrimError(aiError);
        return fallback with { AiError = trimmedError };
    }

    private static bool IsContentValid(string content)
        => !string.IsNullOrWhiteSpace(content) && content.Length <= 1024;

    private static string? TrimError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "AI niedostepne - uzyto szablonu.";

        return message.Length <= 512 ? message : message[..512];
    }
}
