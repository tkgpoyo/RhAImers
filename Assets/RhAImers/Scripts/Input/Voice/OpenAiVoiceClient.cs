using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RhAImers.Input.Voice
{
    /// <summary>
    /// OpenAI の 音声文字起こしAPI（gpt-transcribe）を呼び出すクライアント．
    /// UnityWebRequest を使用して安定したマルチパート通信を行います．
    /// </summary>
    public static class OpenAiVoiceClient
    {
        private const string TranscriptionsEndpoint = "https://api.openai.com/v1/audio/transcriptions";

        public static async UniTask<string> TranscribeAudioAsync(
            byte[] wavBytes,
            string apiKey,
            string model = "gpt-transcribe",
            string language = "ja",
            CancellationToken ct = default)
        {
            if (wavBytes == null || wavBytes.Length == 0)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API key must not be empty.", nameof(apiKey));
            }

            var formData = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("file", wavBytes, "audio.wav", "audio/wav"),
                new MultipartFormDataSection("model", model)
            };

            if (!string.IsNullOrWhiteSpace(language))
            {
                formData.Add(new MultipartFormDataSection("language", language));
            }

            using var uwr = UnityWebRequest.Post(TranscriptionsEndpoint, formData);
            uwr.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            await uwr.SendWebRequest().WithCancellation(ct);

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                string errorBody = uwr.downloadHandler != null ? uwr.downloadHandler.text : string.Empty;
                throw new System.Net.Http.HttpRequestException(
                    $"[OpenAiVoiceClient] API error {(int)uwr.responseCode}: {errorBody} (Detail: {uwr.error})");
            }

            string responseBody = uwr.downloadHandler != null ? uwr.downloadHandler.text : string.Empty;
            return ExtractTextFromJson(responseBody);
        }

        private static string ExtractTextFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return string.Empty;
            }

            try
            {
                var response = JsonUtility.FromJson<TranscriptionResponse>(json);
                if (response != null && !string.IsNullOrEmpty(response.text))
                {
                    return response.text;
                }
            }
            catch
            {
                // JsonUtility が失敗した場合はフォールバック
            }

            const string pattern = "\"text\":";
            int startIndex = json.IndexOf(pattern, StringComparison.Ordinal);
            if (startIndex < 0) return string.Empty;

            startIndex += pattern.Length;
            while (startIndex < json.Length && (json[startIndex] == ' ' || json[startIndex] == '\t'))
            {
                startIndex++;
            }

            if (startIndex >= json.Length || json[startIndex] != '"')
            {
                return string.Empty;
            }

            startIndex++; // skip opening quote
            var sb = new StringBuilder();
            bool isEscaped = false;

            for (int i = startIndex; i < json.Length; i++)
            {
                char c = json[i];
                if (isEscaped)
                {
                    if (c == 'n') sb.Append('\n');
                    else if (c == 'r') sb.Append('\r');
                    else if (c == 't') sb.Append('\t');
                    else sb.Append(c);
                    isEscaped = false;
                }
                else if (c == '\\')
                {
                    isEscaped = true;
                }
                else if (c == '"')
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        [Serializable]
        private class TranscriptionResponse
        {
            public string text;
        }
    }
}
