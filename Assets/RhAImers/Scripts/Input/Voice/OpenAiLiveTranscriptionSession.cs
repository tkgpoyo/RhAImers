using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RhAImers.Input.Voice
{
    /// <summary>
    /// OpenAI Realtime API による gpt-live-transcribe 接続セッションを管理するクラス．
    /// ターン開始時にあらかじめ常時接続（プレコネクト）しておき，低遅延でリアルタイム文字起こしを行います．
    /// </summary>
    public class OpenAiLiveTranscriptionSession : IDisposable
    {
        private const string RealtimeEndpoint = "wss://api.openai.com/v1/realtime?intent=transcription";
        private readonly ClientWebSocket _ws = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Action<string> _onDeltaReceived;
        private readonly Action<string> _onCompleted;
        private readonly StringBuilder _accumulatedTranscript = new();
        private readonly ConcurrentQueue<byte[]> _pendingAudioChunks = new();

        private UniTaskCompletionSource<string> _completionSource;
        private bool _isConnecting;
        private bool _isConnected;
        private bool _isDisposed;

        public bool IsConnected => _isConnected && _ws.State == WebSocketState.Open;
        public string CurrentTranscript => _accumulatedTranscript.ToString();

        public OpenAiLiveTranscriptionSession(Action<string> onDeltaReceived, Action<string> onCompleted)
        {
            _onDeltaReceived = onDeltaReceived;
            _onCompleted = onCompleted;
        }

        public async UniTask ConnectAsync(string apiKey, int sampleRate = 24000, CancellationToken ct = default)
        {
            if (_isDisposed || _isConnected) return;
            _isConnecting = true;

            int rate = Math.Min(sampleRate, 24000);

            try
            {
                _ws.Options.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                var uri = new Uri(RealtimeEndpoint);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
                await _ws.ConnectAsync(uri, linkedCts.Token);

                _isConnected = true;
                _isConnecting = false;
                Debug.Log("[OpenAiLiveTranscriptionSession] WebSocket接続に成功しました．");

                // session.update を送信して gpt-live-transcribe を指定
                string sessionUpdateJson = $@"{{
    ""type"": ""session.update"",
    ""session"": {{
        ""type"": ""transcription"",
        ""audio"": {{
            ""input"": {{
                ""format"": {{
                    ""type"": ""audio/pcm"",
                    ""rate"": {rate}
                }},
                ""transcription"": {{
                    ""model"": ""gpt-live-transcribe""
                }},
                ""turn_detection"": null
            }}
        }}
    }}
}}";
                await SendTextAsync(sessionUpdateJson, linkedCts.Token);

                // 接続中にキューイングされていた音声チャンクをフラッシュ送信
                while (_pendingAudioChunks.TryDequeue(out var chunk))
                {
                    await SendAudioChunkDirectAsync(chunk, linkedCts.Token);
                }

                // バックグラウンド受信ループを開始
                ReceiveLoopAsync(_cts.Token).Forget();
            }
            catch (Exception ex)
            {
                _isConnecting = false;
                _isConnected = false;
                Debug.LogError($"[OpenAiLiveTranscriptionSession] WebSocket接続失敗: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 新しい発話のために蓄積テキストと完了タスクをリセットします．
        /// </summary>
        public void PrepareForNewSpeech()
        {
            _accumulatedTranscript.Clear();
            _completionSource = new UniTaskCompletionSource<string>();
        }

        public async UniTask SendAudioChunkAsync(byte[] pcmData, CancellationToken ct = default)
        {
            if (_isDisposed || pcmData == null || pcmData.Length == 0) return;

            if (!IsConnected)
            {
                _pendingAudioChunks.Enqueue(pcmData);
                return;
            }

            await SendAudioChunkDirectAsync(pcmData, ct);
        }

        private async UniTask SendAudioChunkDirectAsync(byte[] pcmData, CancellationToken ct)
        {
            string base64Audio = Convert.ToBase64String(pcmData);
            string eventJson = $"{{\"type\":\"input_audio_buffer.append\",\"audio\":\"{base64Audio}\"}}";
            await SendTextAsync(eventJson, ct);
        }

        /// <summary>
        /// 音声バッファをコミットし，サーバーからの確定文字起こし結果を待機します．
        /// </summary>
        public async UniTask<string> CommitAudioAndWaitResultAsync(float timeoutSec = 3.0f, CancellationToken ct = default)
        {
            if (_isDisposed || !IsConnected)
            {
                return CurrentTranscript;
            }

            string eventJson = "{\"type\":\"input_audio_buffer.commit\"}";
            await SendTextAsync(eventJson, ct);

            if (_completionSource != null)
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token, _cts.Token);
                try
                {
                    return await _completionSource.Task.AttachExternalCancellation(linked.Token);
                }
                catch (OperationCanceledException)
                {
                    return CurrentTranscript;
                }
            }

            return CurrentTranscript;
        }

        private async UniTask SendTextAsync(string text, CancellationToken ct)
        {
            if (_ws.State != WebSocketState.Open) return;

            byte[] bytes = Encoding.UTF8.GetBytes(text);
            var segment = new ArraySegment<byte>(bytes);
            await _ws.SendAsync(segment, WebSocketMessageType.Text, true, ct);
        }

        private async UniTaskVoid ReceiveLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[8192];
            using var ms = new MemoryStream();

            try
            {
                while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    ms.Write(buffer, 0, result.Count);
                    if (result.EndOfMessage)
                    {
                        string message = Encoding.UTF8.GetString(ms.ToArray());
                        ms.SetLength(0);
                        HandleServerMessage(message);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!_isDisposed)
                {
                    Debug.LogWarning($"[OpenAiLiveTranscriptionSession] WebSocket受信例外: {ex.Message}");
                }
            }
        }

        private void HandleServerMessage(string json)
        {
            try
            {
                // エラーイベントの検知
                if (json.Contains("\"type\":\"error\"") || json.Contains("\"type\": \"error\""))
                {
                    Debug.LogError($"[OpenAiLiveTranscriptionSession] サーバーエラー受信: {json}");
                    return;
                }

                // 差分テキストの反映
                if (json.Contains("input_audio_transcription.delta"))
                {
                    string delta = ExtractStringField(json, "delta");
                    if (!string.IsNullOrEmpty(delta))
                    {
                        _accumulatedTranscript.Append(delta);
                        _onDeltaReceived?.Invoke(_accumulatedTranscript.ToString());
                    }
                }
                // 確定イベント
                else if (json.Contains("input_audio_transcription.completed"))
                {
                    string transcript = ExtractStringField(json, "transcript");
                    if (string.IsNullOrEmpty(transcript))
                    {
                        transcript = _accumulatedTranscript.ToString();
                    }
                    _onCompleted?.Invoke(transcript);
                    _completionSource?.TrySetResult(transcript);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenAiLiveTranscriptionSession] メッセージパース例外: {ex.Message}");
            }
        }

        private static string ExtractStringField(string json, string fieldName)
        {
            string pattern = $"\"{fieldName}\":";
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

            startIndex++;
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

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _isConnected = false;

            try
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            catch { }

            try
            {
                if (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.Connecting)
                {
                    _ws.Abort();
                }
                _ws.Dispose();
            }
            catch { }
        }
    }
}
