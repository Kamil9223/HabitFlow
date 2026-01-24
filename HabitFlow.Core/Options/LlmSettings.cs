namespace HabitFlow.Core.Options;

/// <summary>
/// LLM provider settings for AI-generated notifications.
/// </summary>
public sealed class LlmSettings
{
    public const string SectionName = "LlmSettings";

    public string Provider { get; set; } = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxTokens { get; set; } = 150;
    public double Temperature { get; set; } = 0.7;
    public int TimeoutSeconds { get; set; } = 10;
    public int MaxRetries { get; set; } = 2;
    public int MaxDailyRequests { get; set; } = 100;
    public bool Enabled { get; set; } = true;
}
