using HabitFlow.Core.Options;
using Microsoft.Extensions.Options;

namespace HabitFlow.Core.Options.Validation;

public sealed class LlmSettingsValidator : IValidateOptions<LlmSettings>
{
    public ValidateOptionsResult Validate(string? name, LlmSettings options)
    {
        if (options.MaxTokens <= 0)
            return ValidateOptionsResult.Fail("LlmSettings: MaxTokens must be greater than 0.");

        if (options.TimeoutSeconds <= 0)
            return ValidateOptionsResult.Fail("LlmSettings: TimeoutSeconds must be greater than 0.");

        if (options.MaxRetries < 0)
            return ValidateOptionsResult.Fail("LlmSettings: MaxRetries must be 0 or greater.");

        if (options.MaxDailyRequests < 0)
            return ValidateOptionsResult.Fail("LlmSettings: MaxDailyRequests must be 0 or greater.");

        if (options.Temperature < 0 || options.Temperature > 1)
            return ValidateOptionsResult.Fail("LlmSettings: Temperature must be between 0 and 1.");

        if (options.Enabled && string.IsNullOrWhiteSpace(options.ApiKey))
            return ValidateOptionsResult.Fail("LlmSettings: ApiKey is required when LLM is enabled.");

        if (options.Enabled && string.IsNullOrWhiteSpace(options.Model))
            return ValidateOptionsResult.Fail("LlmSettings: Model is required when LLM is enabled.");

        return ValidateOptionsResult.Success;
    }
}
