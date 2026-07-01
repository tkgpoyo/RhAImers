using System;
using System.Net.Http;
using System.Text;
using UnityEngine;

namespace RhAImers.VerseGeneration
{
    /// <summary>
    /// Calls the Google Gemini generateContent API synchronously.
    /// NOTE: Request() blocks the calling thread. Invoke from a background thread
    /// or a coroutine wrapper to avoid freezing the Unity main thread.
    /// </summary>
    public class LlmClient
    {
        private static readonly HttpClient Http = new HttpClient();

        private const string EndpointTemplate =
            "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

        private const string DefaultModel = "gemini-3.1-flash-lite";

        private const string ApiKeyEnvironmentVariable = "GemKey";

        private readonly string _apiKey;
        private readonly string _model;

        private LlmClient(string apiKey, string model = DefaultModel)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key must not be empty.", nameof(apiKey));
            _apiKey = apiKey;
            _model  = model;
        }

        /// <summary>
        /// Builds a client using the API key from the <c>GEMINI_API_KEY</c>
        /// environment variable, so the key never has to live in source control
        /// or a serialized asset.
        /// </summary>
        public static LlmClient CreateFromEnvironment(string model = DefaultModel)
        {
            var apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable, EnvironmentVariableTarget.User);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{ApiKeyEnvironmentVariable}' is not set. " +
                    "Set it to your Gemini API key before starting the game.");
            }

            return new LlmClient(apiKey, model);
        }

        public string Request(string prompt)
        {
            var url     = string.Format(EndpointTemplate, _model, _apiKey);
            var body    = BuildRequestBody(prompt);
            var content = new StringContent(body, Encoding.UTF8, "text/plain");

            using (var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content })
            {
                HttpResponseMessage response;
                string responseBody;
                try
                {
                    response     = Http.SendAsync(request).GetAwaiter().GetResult();
                    responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[LlmClient] HTTP request failed: {ex.Message}");
                    return string.Empty;
                }

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"[LlmClient] API error {(int)response.StatusCode}: {responseBody}");
                    return string.Empty;
                }

                return ExtractText(responseBody);
            }
        }

        private static string BuildRequestBody(string prompt)
        {
            var escaped = prompt
                .Replace("\\", "\\\\")
                .Replace("\"",  "\\\"")
                .Replace("\n",  "\\n")
                .Replace("\r",  "\\r")
                .Replace("\t",  "\\t");

            // {"contents":[{"parts":[{"text":"..."}]}]}
            return $"{{\"contents\":[{{\"parts\":[{{\"text\":\"{escaped}\"}}]}}]}}";
        }

        private static string ExtractText(string json)
        {
            var response = JsonUtility.FromJson<GeminiResponse>(json);
            if (response?.candidates == null || response.candidates.Length == 0)
                return string.Empty;

            var parts = response.candidates[0]?.content?.parts;
            if (parts == null || parts.Length == 0)
                return string.Empty;

            return parts[0].text ?? string.Empty;
        }

        // ---- Gemini response shape ----

        [Serializable]
        private class GeminiResponse
        {
            public Candidate[] candidates;
        }

        [Serializable]
        private class Candidate
        {
            public Content content;
        }

        [Serializable]
        private class Content
        {
            public Part[] parts;
        }

        [Serializable]
        private class Part
        {
            public string text;
        }
    }
}
