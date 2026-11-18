using System;
using System.IO;
using System.Threading;
using Renci.SshNet;
using TMPro;
using UnityEngine;

public class VoiceOutputTestController : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField _inputField;
    [SerializeField]
    private AudioSource _audioSource;

    // Assets/RhAImers/Local/SshCredentials.asset を割り当てる（ローカル専用・コミットされない）
    [SerializeField]
    private SshCredentials _credentials;
    [SerializeField, Tooltip("接続（TCP〜認証）のタイムアウト")]
    private float _connectTimeoutSeconds = 10f;
    [SerializeField, Tooltip("say の実行タイムアウト。初回はモデル読み込みで時間がかかる")]
    private float _commandTimeoutSeconds = 60f;

    private CancellationTokenSource _cts;

    void Awake()
    {
        //SshNetUnityLoggerFactory.Initialize();
        _cts = new CancellationTokenSource();
    }

    void Start()
    {
        _inputField.onSubmit.AddListener(OnSubmit);
    }

    void OnDestroy()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    // async void はイベントハンドラ専用。例外は必ず内部で捕まえる
    private async void OnSubmit(string text)
    {
        try
        {
            var clip = await SayAsync(text, _cts.Token);
            if (clip != null && _audioSource != null) _audioSource.PlayOneShot(clip);
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("SSH: キャンセルまたはタイムアウトしました");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Awaitable<AudioClip> SayAsync(string text, CancellationToken destroyToken)
    {
        if (_credentials == null)
        {
            Debug.LogError("SSH: SshCredentials が未設定です。Assets/RhAImers/Local/SshCredentials.asset を割り当ててください");
            return null;
        }

        var connectTimeout = TimeSpan.FromSeconds(_connectTimeoutSeconds);
        var commandTimeout = TimeSpan.FromSeconds(_commandTimeoutSeconds);
        var connectionInfo = _credentials.CreateConnectionInfo(connectTimeout);

        var commandText = $"ivrc/bin/say {QuoteForShell(text)}";
        Debug.Log($"SSH: {connectionInfo.Username}@{connectionInfo.Host}:{connectionInfo.Port} に接続中…");

        // Unity の Mono では ConnectAsync（内部で DnsEndPoint + BeginConnect）が
        // "None of the discovered or specified addresses match the socket address family." で必ず失敗する。
        // 同期版 Connect()（SocketAsyncEventArgs 経由）は動くので、メインスレッドを止めないよう
        // バックグラウンドスレッドで同期 API を使う。タイムアウトは ConnectionInfo.Timeout / CommandTimeout が効く。
        await Awaitable.BackgroundThreadAsync();
        byte[] wav;
        int? exitStatus;
        string stderr;
        try
        {
            destroyToken.ThrowIfCancellationRequested();
            using var client = new SshClient(connectionInfo);
            client.Connect();

            // say は WAV (PCM16 mono) を stdout に書き出す
            using var command = client.CreateCommand(commandText);
            command.CommandTimeout = commandTimeout;
            // Execute() は stdout を文字列として読み切ってしまうので ExecuteAsync を待つ
            command.ExecuteAsync(destroyToken).GetAwaiter().GetResult();

            // Result は文字列としてデコードしてしまうので、バイナリは OutputStream から読む
            using (var ms = new MemoryStream())
            {
                command.OutputStream.CopyTo(ms);
                wav = ms.ToArray();
            }

            exitStatus = command.ExitStatus;
            stderr = command.Error;
            client.Disconnect();
        }
        finally
        {
            // AudioClip などの Unity API はメインスレッドでしか使えない
            await Awaitable.MainThreadAsync();
        }

        destroyToken.ThrowIfCancellationRequested();
        Debug.Log($"SSH: 終了コード={exitStatus}, WAV={wav.Length} bytes\nstderr: {stderr}");
        if (exitStatus != 0 || wav.Length == 0) return null;

        return WavToAudioClip(wav, "say");
    }

    // POSIX シェル向けにシングルクォートで囲む（' は '\'' に置換）
    private static string QuoteForShell(string s) => "'" + s.Replace("'", "'\\''") + "'";

    /// <summary>16bit PCM の WAV を AudioClip に変換する。</summary>
    private static AudioClip WavToAudioClip(byte[] wav, string clipName)
    {
        if (wav.Length < 12 || BitConverter.ToInt32(wav, 0) != 0x46464952 /* "RIFF" */ ||
            BitConverter.ToInt32(wav, 8) != 0x45564157 /* "WAVE" */)
        {
            throw new FormatException("WAV ではありません");
        }

        int channels = 0, sampleRate = 0, bitsPerSample = 0;
        var pos = 12;
        while (pos + 8 <= wav.Length)
        {
            var chunkId = BitConverter.ToInt32(wav, pos);
            var chunkSize = BitConverter.ToInt32(wav, pos + 4);
            var body = pos + 8;

            if (chunkId == 0x20746D66) // "fmt "
            {
                channels = BitConverter.ToInt16(wav, body + 2);
                sampleRate = BitConverter.ToInt32(wav, body + 4);
                bitsPerSample = BitConverter.ToInt16(wav, body + 14);
            }
            else if (chunkId == 0x61746164) // "data"
            {
                if (bitsPerSample != 16) throw new FormatException($"16bit PCM のみ対応 (bits={bitsPerSample})");

                var size = Math.Min(chunkSize, wav.Length - body);
                var samples = new float[size / 2];
                for (var i = 0; i < samples.Length; i++)
                {
                    samples[i] = BitConverter.ToInt16(wav, body + i * 2) / 32768f;
                }

                var clip = AudioClip.Create(clipName, samples.Length / channels, channels, sampleRate, false);
                clip.SetData(samples, 0);
                return clip;
            }

            pos = body + chunkSize + (chunkSize & 1);
        }

        throw new FormatException("WAV に data チャンクがありません");
    }
}
