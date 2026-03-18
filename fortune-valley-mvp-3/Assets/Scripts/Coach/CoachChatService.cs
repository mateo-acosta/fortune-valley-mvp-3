using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages conversation history and API calls to the LLM for Coach Val.
    /// Plain C# class — coroutines are hosted by the caller (CoachChatUI).
    /// </summary>
    public class CoachChatService
    {
        // ═══════════════════════════════════════════════════════════════
        // JSON STRUCTS (for response parsing via JsonUtility)
        // ═══════════════════════════════════════════════════════════════

        [Serializable]
        public struct ChatResponse
        {
            public Choice[] choices;
        }

        [Serializable]
        public struct Choice
        {
            public ResponseMessage message;
        }

        [Serializable]
        public struct ResponseMessage
        {
            public string role;
            public string content;
        }

        // ═══════════════════════════════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════════════════════════════

        private readonly CoachConfig _config;
        private readonly string _apiKey;
        private readonly List<(string role, string content)> _history = new List<(string, string)>();
        private bool _isProcessing;

        private const string FallbackMessage = "I didn't get a clear answer. Try asking again.";
        private const string NoApiKeyMessage = "Coach Val isn't available. API key not configured.";
        private const string ConnectionErrorMessage = "Coach Val isn't available. Check your connection.";

        /// <summary>
        /// Whether a request is currently in flight.
        /// </summary>
        public bool IsProcessing => _isProcessing;

        // ═══════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════

        public CoachChatService(CoachConfig config, string gameContext)
        {
            _config = config;

            // Try env var first, fall back to config override
            _apiKey = System.Environment.GetEnvironmentVariable(config.EnvVarName);
            if (string.IsNullOrEmpty(_apiKey))
                _apiKey = config.ApiKeyOverride;

            // Build system prompt by injecting game context
            string systemPrompt = config.SystemPromptTemplate.Replace("{GAME_CONTEXT}", gameContext);
            _history.Add(("system", systemPrompt));
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Send a user message and get a response via coroutine.
        /// Callback receives (responseText, isError).
        /// </summary>
        public IEnumerator SendMessage(string userMessage, Action<string, bool> onComplete)
        {
            if (_isProcessing)
            {
                onComplete?.Invoke("Please wait for the current response.", true);
                yield break;
            }

            // Check for API key
            if (string.IsNullOrEmpty(_apiKey))
            {
                onComplete?.Invoke(NoApiKeyMessage, true);
                yield break;
            }

            _isProcessing = true;

            // Add user message to history
            _history.Add(("user", userMessage));

            // Trim history to keep within limits (always keep system message + last N exchanges)
            TrimHistory();

            // Build request JSON
            string requestBody = BuildRequestJson();

            // Make HTTP request
            using (var request = new UnityWebRequest(_config.ApiEndpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
                request.timeout = _config.RequestTimeoutSeconds;

                yield return request.SendWebRequest();

                _isProcessing = false;

                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        // Remove the user message we just added since the request failed
                        if (_history.Count > 1)
                            _history.RemoveAt(_history.Count - 1);

                        // Log the response body for debugging API errors
                        string responseBody = request.downloadHandler?.text;
                        if (!string.IsNullOrEmpty(responseBody))
                        {
                            // Replace newlines so Unity console shows the full JSON on one line
                            string flat = responseBody.Replace("\n", " ").Replace("\r", "");
                            if (flat.Length > 1000) flat = flat.Substring(0, 1000);
                            UnityEngine.Debug.LogWarning("[CoachChat] API error body (" + responseBody.Length + " chars): " + flat);
                        }

                        string errorMsg = request.result == UnityWebRequest.Result.ConnectionError
                            ? ConnectionErrorMessage
                            : $"Coach Val had a problem: {request.error}";

                        onComplete?.Invoke(errorMsg, true);
                        yield break;
                    }

                    // Parse response
                    string responseText = ParseResponse(request.downloadHandler.text);

                    // Add assistant response to history
                    _history.Add(("assistant", responseText));

                    onComplete?.Invoke(responseText, false);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[CoachChat] Exception processing response: {ex.Message}");
                    // Remove the user message since we couldn't complete the exchange
                    if (_history.Count > 1)
                        _history.RemoveAt(_history.Count - 1);
                    onComplete?.Invoke(FallbackMessage, true);
                    yield break;
                }
            }
        }

        /// <summary>
        /// Clear conversation history (keeps system prompt).
        /// </summary>
        public void ClearHistory()
        {
            if (_history.Count > 1)
            {
                var systemMsg = _history[0];
                _history.Clear();
                _history.Add(systemMsg);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // INTERNAL — exposed as internal for testing
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Parse OpenAI Chat Completions response JSON.
        /// Returns the assistant's message content, or a fallback on failure.
        /// </summary>
        public static string ParseResponse(string json)
        {
            if (string.IsNullOrEmpty(json))
                return FallbackMessage;

            try
            {
                var response = JsonUtility.FromJson<ChatResponse>(json);

                if (response.choices == null || response.choices.Length == 0)
                    return FallbackMessage;

                string content = response.choices[0].message.content;
                return string.IsNullOrEmpty(content) ? FallbackMessage : content.Trim();
            }
            catch
            {
                return FallbackMessage;
            }
        }

        /// <summary>
        /// Escape a string for safe inclusion in JSON.
        /// Handles quotes, backslashes, newlines, tabs, and control characters.
        /// </summary>
        public static string EscapeJsonString(string input)
        {
            if (input == null)
                return "null";

            var sb = new StringBuilder(input.Length + 10);
            foreach (char c in input)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        // Escape control characters
                        if (c < 0x20)
                            sb.Append($"\\u{(int)c:X4}");
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Build request JSON manually (system prompt can contain arbitrary text).
        /// </summary>
        private string BuildRequestJson()
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"model\":\"{EscapeJsonString(_config.ModelName)}\",");
            sb.Append($"\"max_tokens\":{_config.MaxTokens},");
            // Format temperature with invariant culture to avoid comma decimals
            sb.Append($"\"temperature\":{_config.Temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
            sb.Append("\"messages\":[");

            for (int i = 0; i < _history.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append("{");
                sb.Append($"\"role\":\"{EscapeJsonString(_history[i].role)}\",");
                sb.Append($"\"content\":\"{EscapeJsonString(_history[i].content)}\"");
                sb.Append("}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>
        /// Trim history to keep system message + last N exchanges.
        /// Each exchange is a user message + assistant response (2 entries).
        /// </summary>
        private void TrimHistory()
        {
            int maxMessages = 1 + (_config.MaxHistoryExchanges * 2); // system + N exchanges × 2
            while (_history.Count > maxMessages)
            {
                // Remove oldest non-system message (index 1)
                _history.RemoveAt(1);
            }
        }

        /// <summary>
        /// Expose history count for testing.
        /// </summary>
        public int HistoryCount => _history.Count;
    }
}
