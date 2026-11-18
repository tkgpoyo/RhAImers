using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace RhAImers.Input.Voice
{
    /// <summary>
    /// 音声サンプルデータをWAV（RIFF WAVE 16-bit PCM）バイト配列に変換するユーティリティクラス．
    /// </summary>
    public static class AudioClipToWav
    {
        public static byte[] EncodeToWav(float[] samples, int channels, int sampleRate)
        {
            if (samples == null || samples.Length == 0)
            {
                return Array.Empty<byte>();
            }

            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);

            const int bitsPerSample = 16;
            int byteRate = sampleRate * channels * (bitsPerSample / 8);
            short blockAlign = (short)(channels * (bitsPerSample / 8));
            int subChunk2Size = samples.Length * (bitsPerSample / 8);
            int chunkSize = 36 + subChunk2Size;

            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(chunkSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt sub-chunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Subchunk1Size (16 for PCM)
            writer.Write((short)1); // AudioFormat (1 for PCM)
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write((short)bitsPerSample);

            // data sub-chunk
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(subChunk2Size);

            // 16-bit PCM samples
            for (int i = 0; i < samples.Length; i++)
            {
                short pcmSample = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                writer.Write(pcmSample);
            }

            return memoryStream.ToArray();
        }
    }
}
