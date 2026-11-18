using System;
using UnityEngine;

namespace RhAImers.Input.Voice
{
    /// <summary>
    /// UnityのMicrophone APIを用いたマイク録音管理クラス．
    /// ターン開始時にスタンバイ録音を走らせておくことで，スペースキー押下時のラグをゼロにします．
    /// マイクが48000Hz等の場合でも，OpenAI仕様に合わせて自動で24000Hzにリサンプリングします．
    /// </summary>
    public class MicrophoneRecorder
    {
        public const int TargetStreamingSampleRate = 24000;

        private AudioClip _recordingClip;
        private string _deviceName;
        private int _sampleRate = 24000;
        private int _lastSamplePosition;
        private int _startSamplePosition;
        private bool _isRecordingActive;
        private bool _isStandby;

        public bool IsRecordingActive => _isRecordingActive;
        public int SampleRate => _sampleRate;
        public int Channels => _recordingClip != null ? _recordingClip.channels : 1;

        /// <summary>
        /// マイクデバイスを初期化し，スタンバイ録音を開始します（ターン開始時に事前実行）．
        /// </summary>
        public bool InitializeAndStartStandby(int sampleRate = 24000, int lengthSec = 60)
        {
            if (_isStandby && _recordingClip != null && Microphone.IsRecording(_deviceName))
            {
                return true;
            }

            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[MicrophoneRecorder] マイクデバイスが見つかりません．");
                return false;
            }

            _deviceName = Microphone.devices[0];

            int minFreq, maxFreq;
            Microphone.GetDeviceCaps(_deviceName, out minFreq, out maxFreq);
            if (minFreq != 0 || maxFreq != 0)
            {
                sampleRate = Mathf.Clamp(sampleRate, minFreq, maxFreq);
            }

            _sampleRate = sampleRate;

            _recordingClip = Microphone.Start(_deviceName, true, lengthSec, _sampleRate);
            if (_recordingClip == null)
            {
                Debug.LogWarning("[MicrophoneRecorder] マイク録音の開始に失敗しました．");
                return false;
            }

            _isStandby = true;
            _isRecordingActive = false;
            return true;
        }

        /// <summary>
        /// スペース押下時：発話キャプチャを開始します（位置記録のみのためラグはゼロ）．
        /// </summary>
        public void BeginVoiceCapture()
        {
            if (!_isStandby || _recordingClip == null || !Microphone.IsRecording(_deviceName))
            {
                InitializeAndStartStandby(_sampleRate);
            }

            _startSamplePosition = Microphone.GetPosition(_deviceName);
            _lastSamplePosition = _startSamplePosition;
            _isRecordingActive = true;
        }

        /// <summary>
        /// スペース長押し中：前回取得以降の新規サンプルを 24000Hz 16bit PCMバイト配列として取得します．
        /// </summary>
        public byte[] GetNewAudioPcmChunk()
        {
            if (!_isRecordingActive || _recordingClip == null)
            {
                return Array.Empty<byte>();
            }

            int currentPosition = Microphone.GetPosition(_deviceName);
            if (currentPosition < 0 || currentPosition == _lastSamplePosition)
            {
                return Array.Empty<byte>();
            }

            int totalClipSamples = _recordingClip.samples;
            int sampleCount;
            if (currentPosition >= _lastSamplePosition)
            {
                sampleCount = currentPosition - _lastSamplePosition;
            }
            else
            {
                sampleCount = (totalClipSamples - _lastSamplePosition) + currentPosition;
            }

            if (sampleCount <= 0)
            {
                return Array.Empty<byte>();
            }

            float[] samples = new float[sampleCount * _recordingClip.channels];

            if (currentPosition >= _lastSamplePosition)
            {
                _recordingClip.GetData(samples, _lastSamplePosition);
            }
            else
            {
                int firstChunk = totalClipSamples - _lastSamplePosition;
                float[] firstPart = new float[firstChunk * _recordingClip.channels];
                _recordingClip.GetData(firstPart, _lastSamplePosition);
                Array.Copy(firstPart, 0, samples, 0, firstPart.Length);

                if (currentPosition > 0)
                {
                    float[] secondPart = new float[currentPosition * _recordingClip.channels];
                    _recordingClip.GetData(secondPart, 0);
                    Array.Copy(secondPart, 0, samples, firstPart.Length, secondPart.Length);
                }
            }

            _lastSamplePosition = currentPosition;

            // マイク周波数が 24000Hz でない場合は 24000Hz へリサンプリング
            if (_sampleRate != TargetStreamingSampleRate)
            {
                samples = Resample(samples, _sampleRate, TargetStreamingSampleRate);
            }

            return ConvertFloatToPcm16(samples);
        }

        /// <summary>
        /// スペース離脱時：押下中だった区間の全サンプルを切り出して返します．マイクはスタンバイを継続します．
        /// </summary>
        public float[] EndVoiceCapture()
        {
            if (!_isRecordingActive || _recordingClip == null)
            {
                _isRecordingActive = false;
                return Array.Empty<float>();
            }

            int endPosition = Microphone.GetPosition(_deviceName);
            _isRecordingActive = false;

            int totalClipSamples = _recordingClip.samples;
            int sampleCount;
            if (endPosition >= _startSamplePosition)
            {
                sampleCount = endPosition - _startSamplePosition;
            }
            else
            {
                sampleCount = (totalClipSamples - _startSamplePosition) + endPosition;
            }

            if (sampleCount <= 0)
            {
                return Array.Empty<float>();
            }

            float[] samples = new float[sampleCount * _recordingClip.channels];
            if (endPosition >= _startSamplePosition)
            {
                _recordingClip.GetData(samples, _startSamplePosition);
            }
            else
            {
                int firstChunk = totalClipSamples - _startSamplePosition;
                float[] firstPart = new float[firstChunk * _recordingClip.channels];
                _recordingClip.GetData(firstPart, _startSamplePosition);
                Array.Copy(firstPart, 0, samples, 0, firstPart.Length);

                if (endPosition > 0)
                {
                    float[] secondPart = new float[endPosition * _recordingClip.channels];
                    _recordingClip.GetData(secondPart, 0);
                    Array.Copy(secondPart, 0, samples, firstPart.Length, secondPart.Length);
                }
            }

            return samples;
        }

        /// <summary>
        /// ターン終了時や入力無効化時にマイクを停止します．
        /// </summary>
        public void StopStandby()
        {
            _isRecordingActive = false;
            if (_isStandby)
            {
                if (!string.IsNullOrEmpty(_deviceName))
                {
                    Microphone.End(_deviceName);
                }
                _isStandby = false;
            }
        }

        public static float[] Resample(float[] input, int sourceRate, int targetRate)
        {
            if (input == null || input.Length == 0) return Array.Empty<float>();
            if (sourceRate == targetRate) return input;

            double ratio = (double)sourceRate / targetRate;
            int outputLength = (int)Math.Round(input.Length / ratio);
            if (outputLength <= 0) return Array.Empty<float>();

            float[] output = new float[outputLength];
            for (int i = 0; i < outputLength; i++)
            {
                double inputIndex = i * ratio;
                int indexLow = (int)Math.Floor(inputIndex);
                int indexHigh = Math.Min(indexLow + 1, input.Length - 1);
                double fraction = inputIndex - indexLow;

                output[i] = (float)((1.0 - fraction) * input[indexLow] + fraction * input[indexHigh]);
            }
            return output;
        }

        public static byte[] ConvertFloatToPcm16(float[] samples)
        {
            byte[] pcm = new byte[samples.Length * 2];
            int pcmIndex = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                short val = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                pcm[pcmIndex++] = (byte)(val & 0xFF);
                pcm[pcmIndex++] = (byte)((val >> 8) & 0xFF);
            }
            return pcm;
        }
    }
}
