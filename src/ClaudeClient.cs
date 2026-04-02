using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Configuration;

namespace Gaffer;

/// <summary>Handles all Anthropic Claude API communication for Gaffer.</summary>
public sealed class ClaudeClient
{
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-sonnet-4-20250514";
    private const string AnthropicVersion = "2023-06-01";
    private const int MaxTokens = 1000;

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(45) };
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly ConfigEntry<string> _apiKey;

    public ClaudeClient(ConfigEntry<string> apiKey)
    {
        _apiKey = apiKey;
    }

    /// <summary>Sends game context and user prompt to Claude and returns the assistant response text.</summary>
    public async Task<string> SendAnalysisAsync(string gameStateJson, string userPrompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey.Value))
        {
            throw new InvalidOperationException("Claude API key is empty in BepInEx config.");
        }

        var systemPrompt = "You are Gaffer, an FM26 coaching analyst. Use the structured game state JSON to give actionable tactical and squad advice.\n\nGAME_STATE_JSON:\n" + gameStateJson;

        var payload = new ClaudeRequest(
            Model,
            MaxTokens,
            systemPrompt,
            new[] { new ClaudeMessage("user", userPrompt) });

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };

        request.Headers.Add("x-api-key", _apiKey.Value);
        request.Headers.Add("anthropic-version", AnthropicVersion);

        var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Claude API error {(int)response.StatusCode}: {responseBody}");
        }

        using var document = JsonDocument.Parse(responseBody);
        if (document.RootElement.TryGetProperty("content", out JsonElement content) && content.GetArrayLength() > 0)
        {
            JsonElement first = content[0];
            if (first.TryGetProperty("text", out JsonElement textElement))
            {
                return textElement.GetString() ?? string.Empty;
            }
        }

        return responseBody;
    }

    private record ClaudeRequest(string Model, int MaxTokens, string System, ClaudeMessage[] Messages);
    private record ClaudeMessage(string Role, string Content);
}
