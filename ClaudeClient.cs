using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Gaffer
{
    /// <summary>
    /// All Claude API communication is isolated here. Nothing else calls the API.
    /// Sends structured game state to Claude and returns the response text.
    /// </summary>
    internal static class ClaudeClient
    {
        // HttpClient is thread-safe and intended to be reused across requests.
        // IL2CPP PATTERN: HttpClient in .NET 6 works normally inside BepInEx 6 plugins —
        // BepInEx runs a full .NET 6 CLR alongside the IL2CPP native process, so all
        // standard .NET HTTP APIs are available without any IL2CPP-specific workarounds.
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Sends a user message to Claude with a system prompt containing game state JSON.
        /// Returns the response text, or throws on HTTP/API error.
        /// </summary>
        public static async Task<string> SendAsync(
            string apiKey,
            string systemPrompt,
            string userMessage)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("API key is not configured.");

            var requestBody = new ClaudeRequest
            {
                Model     = Constants.ClaudeModel,
                MaxTokens = Constants.ClaudeMaxTokens,
                System    = systemPrompt,
                Messages  = new[]
                {
                    new ClaudeMessage { Role = "user", Content = userMessage }
                }
            };

            var json    = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, Constants.ClaudeApiUrl)
            {
                Content = content
            };

            // Anthropic authentication headers
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", Constants.ClaudeApiVersion);
            // Accept header for JSON is set via content-type above; add explicit accept
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var httpResponse = await _http.SendAsync(request).ConfigureAwait(false);

            var responseBody = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                // Surface the Anthropic error body so the user can diagnose API key/quota issues
                throw new HttpRequestException(
                    $"Claude API returned {(int)httpResponse.StatusCode}: {responseBody}");
            }

            return ParseResponseText(responseBody);
        }

        // ── Response parsing ──────────────────────────────────────────────────────

        private static string ParseResponseText(string json)
        {
            try
            {
                var response = JsonSerializer.Deserialize<ClaudeResponse>(json, JsonOptions);
                if (response?.Content is { Length: > 0 } && response.Content[0].Text != null)
                    return response.Content[0].Text!;

                return "(no text content in response)";
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to parse Claude response: {ex.Message}\nRaw: {json}");
            }
        }

        // ── JSON options ──────────────────────────────────────────────────────────

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented               = false
        };

        // ── Request / response DTO types ──────────────────────────────────────────

        private sealed class ClaudeRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = Constants.ClaudeModel;

            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; set; } = Constants.ClaudeMaxTokens;

            [JsonPropertyName("system")]
            public string? System { get; set; }

            [JsonPropertyName("messages")]
            public ClaudeMessage[] Messages { get; set; } = Array.Empty<ClaudeMessage>();
        }

        private sealed class ClaudeMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = "user";

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        private sealed class ClaudeResponse
        {
            [JsonPropertyName("content")]
            public ClaudeContentBlock[]? Content { get; set; }

            [JsonPropertyName("stop_reason")]
            public string? StopReason { get; set; }
        }

        private sealed class ClaudeContentBlock
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}
