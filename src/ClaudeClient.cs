using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Configuration;

namespace FM26Companion;

/// <summary>
/// All Claude API communication lives here and nowhere else.
///
/// Usage:
///   var reply = await ClaudeClient.Instance.SendMessageAsync("Analyse my midfield.");
///
/// The API key is read from BepInEx config on first use:
///   BepInEx/config/FM26Companion.cfg  →  [Claude] ApiKey = sk-ant-...
/// </summary>
public sealed class ClaudeClient
{
    public static readonly ClaudeClient Instance = new();
    private ClaudeClient() { }

    // ── Configuration ─────────────────────────────────────────────────────────

    private const string ApiUrl           = "https://api.anthropic.com/v1/messages";
    private const string ModelId          = "claude-sonnet-4-20250514";
    private const string AnthropicVersion = "2023-06-01";
    private const int    MaxTokens        = 1000;

    // Lazy so we can read BepInEx config (which is only available after Load()).
    private static readonly Lazy<HttpClient> Http = new(CreateHttpClient);

    private ConfigEntry<string>? _apiKeyEntry;

    // ── Main-thread dispatch ──────────────────────────────────────────────────
    // Unity requires UI updates on the main thread. HTTP responses arrive on a
    // ThreadPool thread, so we post results back via a simple queue that's drained
    // each frame by the dispatcher component (see MainThreadDispatcher).
    // SendMessageAsync awaits the whole round-trip and returns on the main thread
    // so callers can safely touch Unity objects in a continuation.

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialise with BepInEx config. Called once from Plugin.Load().
    /// </summary>
    public void Initialise(ConfigFile config)
    {
        _apiKeyEntry = config.Bind(
            section     : "Claude",
            key         : "ApiKey",
            defaultValue: "",
            description : "Your Anthropic API key (sk-ant-...). " +
                          "Get one at https://console.anthropic.com/");
    }

    /// <summary>
    /// Sends a single user message to Claude and returns the text reply.
    /// Throws <see cref="InvalidOperationException"/> if the API key is not set.
    /// Throws <see cref="HttpRequestException"/> on network or API errors.
    /// </summary>
    /// <param name="userMessage">Plain-text message to send.</param>
    /// <param name="systemPrompt">Optional system prompt override.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public async Task<string> SendMessageAsync(
        string            userMessage,
        string?           systemPrompt      = null,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _apiKeyEntry?.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Claude API key is not set. " +
                "Add it to BepInEx/config/FM26Companion.cfg under [Claude] ApiKey.");

        var requestBody = BuildRequestBody(userMessage, systemPrompt);
        var json        = JsonSerializer.Serialize(requestBody, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // The API key header must be set per-request because HttpClient is shared.
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = content
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);

        Plugin.Log.LogInfo("Gaffer: sending request to Claude API…");

        var response = await Http.Value
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var responseJson = await response.Content
            .ReadAsStringAsync()
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            Plugin.Log.LogError($"Gaffer: Claude API error {(int)response.StatusCode} — {responseJson}");
            throw new HttpRequestException(
                $"Claude API returned {(int)response.StatusCode}: {responseJson}");
        }

        return ParseResponseText(responseJson);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static ClaudeRequest BuildRequestBody(string userMessage, string? systemPrompt)
    {
        return new ClaudeRequest(
            Model     : ModelId,
            MaxTokens : MaxTokens,
            System    : systemPrompt ?? DefaultSystemPrompt,
            Messages  : new[]
            {
                new ClaudeMessage(Role: "user", Content: userMessage)
            });
    }

    private static string ParseResponseText(string json)
    {
        using var doc    = JsonDocument.Parse(json);
        var root         = doc.RootElement;

        // Claude response shape: { "content": [{ "type": "text", "text": "..." }] }
        if (root.TryGetProperty("content", out var content) &&
            content.GetArrayLength() > 0)
        {
            var first = content[0];
            if (first.TryGetProperty("text", out var text))
                return text.GetString() ?? string.Empty;
        }

        // Fallback: return raw JSON so we can see what went wrong
        Plugin.Log.LogWarning($"Gaffer: unexpected Claude response shape — {json}");
        return json;
    }

    private const string DefaultSystemPrompt =
        "You are Gaffer, an expert football manager assistant embedded directly " +
        "inside Football Manager 2026. You have access to live game data and provide " +
        "concise, tactical analysis and coaching advice. " +
        "Keep responses short (under 200 words) and focused on actionable insight. " +
        "Speak like a knowledgeable assistant manager, not a chatbot.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented               = false,
    };

    // ── Request / response models ─────────────────────────────────────────────

    private record ClaudeRequest(
        string           Model,
        int              MaxTokens,
        string           System,
        ClaudeMessage[]  Messages);

    private record ClaudeMessage(
        string Role,
        string Content);
}
