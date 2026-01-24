using HabitFlow.Core.Common;

namespace HabitFlow.Core.Abstractions.Notifications;

/// <summary>
/// Abstraction for LLM providers.
/// </summary>
public interface ILlmClient
{
    Task<Result<string>> GenerateCompletionAsync(
        LlmCompletionRequest request,
        CancellationToken cancellationToken);
}
