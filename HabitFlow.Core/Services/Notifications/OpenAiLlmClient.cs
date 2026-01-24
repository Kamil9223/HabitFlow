using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HabitFlow.Core.Abstractions.Notifications;
using HabitFlow.Core.Common;
using HabitFlow.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;

namespace HabitFlow.Core.Services.Notifications;

/// <summary>
/// OpenAI LLM client using the chat completions API.
/// </summary>
public sealed class OpenAiLlmClient(
    HttpClient httpClient,
    IOptions<LlmSettings> options,
    ILogger<OpenAiLlmClient> logger) : ILlmClient
{
    private static readonly Uri DefaultBaseUri = new("https://api.openai.com/v1/");
    private readonly LlmSettings _settings = options.Value;

    public async Task<Result<string>> GenerateCompletionAsync(
        LlmCompletionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return Result.Failure<string>(
                Error.Failure("Llm.ApiKeyMissing", "Brak skonfigurowanego klucza API dla LLM."));
        }

        httpClient.BaseAddress ??= DefaultBaseUri;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(request.Timeout);

        var payload = new OpenAiChatRequest
        {
            Model = _settings.Model,
            Messages =
            [
                new OpenAiMessage("system", request.SystemPrompt),
                new OpenAiMessage("user", request.UserPrompt)
            ],
            MaxTokens = request.MaxTokens,
            Temperature = request.Temperature
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, SerializerOptions), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        try
        {
            using var response = await httpClient.SendAsync(httpRequest, cts.Token);
            var body = await response.Content.ReadAsStringAsync(cts.Token);

            if (!response.IsSuccessStatusCode)
                return Result.Failure<string>(BuildError(response.StatusCode, body));

            try
            {
                var parsed = JsonSerializer.Deserialize<OpenAiChatResponse>(body, SerializerOptions);
                var content = parsed?.Choices.FirstOrDefault()?.Message?.Content;

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Result.Failure<string>(
                        Error.Failure("Llm.EmptyResponse", "LLM zwrocilo pusta odpowiedz."));
                }

                return Result.Success(content.Trim());
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to parse LLM response.");
                return Result.Failure<string>(
                    Error.Failure("Llm.InvalidResponse", "Niepoprawna odpowiedz z LLM."));
            }
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning(ex, "LLM circuit breaker is open.");
            return Result.Failure<string>(
                Error.Failure("Llm.CircuitOpen", "LLM jest tymczasowo niedostepne (circuit breaker)."));
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "LLM request timed out.");
            return Result.Failure<string>(
                Error.Failure("Llm.Timeout", "Przekroczono limit czasu zapytania do LLM."));
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "LLM request failed.");
            return Result.Failure<string>(
                Error.Failure("Llm.RequestFailed", "Nie udalo sie wykonac zapytania do LLM."));
        }
    }

    private static Error BuildError(
        HttpStatusCode statusCode,
        string body)
    {
        var message = "LLM zwrocilo blad.";
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var error = JsonSerializer.Deserialize<OpenAiErrorEnvelope>(body, SerializerOptions);
                if (!string.IsNullOrWhiteSpace(error?.Error?.Message))
                    message = error.Error.Message;
            }
            catch (JsonException)
            {
                // Keep default message for non-JSON bodies.
            }
        }
        var code = $"Llm.Http{(int)statusCode}";
        return Error.Failure(code, message);
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed class OpenAiChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenAiMessage> Messages { get; init; } = [];

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; init; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; init; }
    }

    private sealed record OpenAiMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed class OpenAiChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice> Choices { get; init; } = [];
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiMessage? Message { get; init; }
    }

    private sealed class OpenAiErrorEnvelope
    {
        [JsonPropertyName("error")]
        public OpenAiError? Error { get; init; }
    }

    private sealed class OpenAiError
    {
        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }
}
