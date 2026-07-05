using System.Collections;
using UnityEngine;

namespace RhAImers.UI
{
    public class BattleBgmPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private AudioSource _audioSource;

        [Header("BGM")]
        [SerializeField] private AudioClip _battleBgmClip;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.45f;
        [SerializeField] private bool _loop = true;

        [Header("Timing")]
        [SerializeField] private bool _playOnBattleStartSignal = true;
        [SerializeField] private bool _playOnSceneStart = false;
        [SerializeField] private bool _restartOnRetry = true;
        [SerializeField] private bool _stopOnBattleResult = true;

        [Header("Fade")]
        [SerializeField] private float _fadeInDuration = 0.75f;
        [SerializeField] private float _fadeOutDuration = 0.75f;

        private Coroutine _fadeCoroutine;

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.playOnAwake = false;
            _audioSource.loop = _loop;
            _audioSource.volume = 0f;
            _audioSource.spatialBlend = 0f;
        }

        private void Start()
        {
            if (_playOnSceneStart)
            {
                PlayBgm(true);
            }
        }

        private void OnEnable()
        {
            if (_uiManager == null)
            {
                return;
            }

            _uiManager.BattleStartSignalShown += HandleBattleStartSignalShown;
            _uiManager.BattleResultShown += HandleBattleResultShown;
            _uiManager.RetrySelected += HandleRetrySelected;
        }

        private void OnDisable()
        {
            if (_uiManager != null)
            {
                _uiManager.BattleStartSignalShown -= HandleBattleStartSignalShown;
                _uiManager.BattleResultShown -= HandleBattleResultShown;
                _uiManager.RetrySelected -= HandleRetrySelected;
            }

            StopFadeCoroutine();
        }

        public void PlayBgm(bool restart)
        {
            if (_audioSource == null || _battleBgmClip == null)
            {
                return;
            }

            if (_audioSource.clip != _battleBgmClip)
            {
                _audioSource.clip = _battleBgmClip;
                restart = true;
            }

            _audioSource.loop = _loop;

            if (restart)
            {
                _audioSource.Stop();
                _audioSource.time = 0f;
            }

            if (!_audioSource.isPlaying)
            {
                _audioSource.volume = 0f;
                _audioSource.Play();
            }

            FadeTo(_volume, _fadeInDuration);
        }

        public void StopBgm()
        {
            if (_audioSource == null)
            {
                return;
            }

            FadeTo(0f, _fadeOutDuration, stopAfterFade: true);
        }

        private void HandleBattleStartSignalShown()
        {
            if (!_playOnBattleStartSignal)
            {
                return;
            }

            PlayBgm(true);
        }

        private void HandleBattleResultShown()
        {
            if (!_stopOnBattleResult)
            {
                return;
            }

            StopBgm();
        }

        private void HandleRetrySelected()
        {
            if (!_restartOnRetry)
            {
                return;
            }

            PlayBgm(true);
        }

        private void FadeTo(float targetVolume, float duration, bool stopAfterFade = false)
        {
            StopFadeCoroutine();
            _fadeCoroutine = StartCoroutine(FadeRoutine(Mathf.Clamp01(targetVolume), Mathf.Max(0f, duration), stopAfterFade));
        }

        private IEnumerator FadeRoutine(float targetVolume, float duration, bool stopAfterFade)
        {
            if (_audioSource == null)
            {
                yield break;
            }

            float startVolume = _audioSource.volume;

            if (duration <= 0f)
            {
                _audioSource.volume = targetVolume;

                if (stopAfterFade && targetVolume <= 0f)
                {
                    _audioSource.Stop();
                }

                _fadeCoroutine = null;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseInOutCubic(t);
                _audioSource.volume = Mathf.Lerp(startVolume, targetVolume, eased);
                yield return null;
            }

            _audioSource.volume = targetVolume;

            if (stopAfterFade && targetVolume <= 0f)
            {
                _audioSource.Stop();
            }

            _fadeCoroutine = null;
        }

        private void StopFadeCoroutine()
        {
            if (_fadeCoroutine == null)
            {
                return;
            }

            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        private float EaseInOutCubic(float t)
        {
            if (t < 0.5f)
            {
                return 4f * t * t * t;
            }

            float f = -2f * t + 2f;
            return 1f - (f * f * f) / 2f;
        }
    }
}
