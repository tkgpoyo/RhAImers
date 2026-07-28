using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Battle End Result Reveal Effect")]
    public sealed class BattleEndResultRevealEffect : MonoBehaviour
    {
        [Header("Dim Overlay")]
        [SerializeField] private Image _dimImage;
        [SerializeField] private Color _dimColor = Color.black;
        [SerializeField] private float _dimTargetAlpha = 0.55f;
        [SerializeField] private float _dimFadeDuration = 0.75f;

        [Header("Content")]
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private CanvasGroup _contentCanvasGroup;
        [SerializeField] private Vector2 _contentStartOffset = new Vector2(0f, -36f);
        [SerializeField] private float _contentDelayAfterDim = 0.08f;
        [SerializeField] private float _contentRevealDuration = 0.45f;
        [SerializeField] private bool _captureFinalPositionInEditMode = true;
        [SerializeField] private bool _blockInputUntilContentShown = true;

        [Header("Panel Style")]
        [SerializeField] private bool _useHologramJitter = true;
        [SerializeField] private float _glitchOffset = 10f;
        [SerializeField] private float _glitchInterval = 0.035f;
        [SerializeField] private float _flickerStrength = 0.26f;
        [SerializeField] private bool _useFlash = true;
        [SerializeField] private Color _flashColor = new Color(0.2f, 1f, 1f, 1f);
        [SerializeField] private float _flashStrength = 0.42f;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _dimStartClip;
        [SerializeField] private AudioClip _contentRevealClip;
        [SerializeField] private float _dimStartVolume = 0.75f;
        [SerializeField] private float _contentRevealVolume = 0.9f;

        [Header("Timing")]
        [SerializeField] private bool _playOnEnable = true;
        [SerializeField] private bool _useUnscaledTime = true;

        private Coroutine _revealCoroutine;
        private Vector2 _contentFinalPosition;
        private Vector2 _currentJitterOffset;
        private float _nextJitterTime;
        private bool _hasContentFinalPosition;
        private Graphic[] _contentGraphics;
        private Color[] _baseGraphicColors;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            CaptureContentFinalPositionIfNeeded(force: true);
            CaptureGraphicColors();
            ApplyHiddenState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CaptureContentFinalPositionIfNeeded(force: ShouldForceCapturePosition());
            CaptureGraphicColors();

            if (_playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            StopRevealCoroutine();
            RestoreGraphicColors();
        }

        [ContextMenu("Play")]
        public void Play()
        {
            StopRevealCoroutine();
            ResolveReferences();
            CaptureContentFinalPositionIfNeeded(force: ShouldForceCapturePosition());
            CaptureGraphicColors();
            ApplyHiddenState();
            _revealCoroutine = StartCoroutine(RevealRoutine());
        }

        [ContextMenu("Capture Current Content Position As Final")]
        public void CaptureCurrentContentPositionAsFinal()
        {
            ResolveReferences();
            CaptureContentFinalPositionIfNeeded(force: true);
        }

        private IEnumerator RevealRoutine()
        {
            PlayOneShot(_dimStartClip, _dimStartVolume);

            yield return FadeDimRoutine(0f, _dimTargetAlpha, _dimFadeDuration);

            if (_contentDelayAfterDim > 0f)
            {
                yield return Wait(_contentDelayAfterDim);
            }

            PlayOneShot(_contentRevealClip, _contentRevealVolume);
            yield return RevealContentRoutine();

            SetContentInteractable(true);
            RestoreGraphicColors();
            _revealCoroutine = null;
        }

        private IEnumerator FadeDimRoutine(float from, float to, float duration)
        {
            if (_dimImage == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                SetDimAlpha(to);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Clamp01(elapsed / duration);
                float eased = EaseInOutCubic(t);
                SetDimAlpha(Mathf.Lerp(from, to, eased));
                yield return null;
            }

            SetDimAlpha(to);
        }

        private IEnumerator RevealContentRoutine()
        {
            if (_contentRoot == null)
            {
                yield break;
            }

            if (_contentRevealDuration <= 0f)
            {
                SetContentAlpha(1f);
                _contentRoot.anchoredPosition = _contentFinalPosition;
                yield break;
            }

            float elapsed = 0f;
            _nextJitterTime = 0f;
            _currentJitterOffset = Vector2.zero;

            while (elapsed < _contentRevealDuration)
            {
                elapsed += GetDeltaTime();
                float t = Clamp01(elapsed / _contentRevealDuration);
                float eased = EaseOutCubic(t);
                float edge = 1f - t;

                Vector2 basePosition = Vector2.Lerp(
                    _contentFinalPosition + _contentStartOffset,
                    _contentFinalPosition,
                    eased);

                Vector2 jitter = _useHologramJitter
                    ? GetJitterOffset(edge)
                    : Vector2.zero;

                _contentRoot.anchoredPosition = basePosition + jitter;
                SetContentAlpha(GetFlickeredAlpha(eased, edge));
                ApplyFlash(edge);

                yield return null;
            }

            _contentRoot.anchoredPosition = _contentFinalPosition;
            SetContentAlpha(1f);
        }

        private void ResolveReferences()
        {
            if (_dimImage == null)
            {
                _dimImage = GetComponent<Image>();
            }

            if (_contentCanvasGroup == null && _contentRoot != null)
            {
                _contentCanvasGroup = _contentRoot.GetComponent<CanvasGroup>();
            }

            if (_contentCanvasGroup == null && _contentRoot != null)
            {
                _contentCanvasGroup = _contentRoot.gameObject.AddComponent<CanvasGroup>();
            }

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            if (_audioSource == null)
            {
                _audioSource = GetComponentInParent<AudioSource>();
            }

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (_audioSource != null)
            {
                _audioSource.playOnAwake = false;
                _audioSource.loop = false;
                _audioSource.spatialBlend = 0f;
            }
        }

        private void CaptureContentFinalPositionIfNeeded(bool force)
        {
            if (_contentRoot == null)
            {
                return;
            }

            if (_hasContentFinalPosition && !force)
            {
                return;
            }

            _contentFinalPosition = _contentRoot.anchoredPosition;
            _hasContentFinalPosition = true;
        }

        private bool ShouldForceCapturePosition()
        {
            return _captureFinalPositionInEditMode && !Application.isPlaying;
        }

        private void CaptureGraphicColors()
        {
            if (_contentRoot == null)
            {
                _contentGraphics = null;
                _baseGraphicColors = null;
                return;
            }

            _contentGraphics = _contentRoot.GetComponentsInChildren<Graphic>(true);
            _baseGraphicColors = new Color[_contentGraphics.Length];

            for (int i = 0; i < _contentGraphics.Length; i++)
            {
                _baseGraphicColors[i] = _contentGraphics[i] != null
                    ? _contentGraphics[i].color
                    : Color.white;
            }
        }

        private void ApplyHiddenState()
        {
            SetDimAlpha(0f);

            if (_contentRoot != null)
            {
                _contentRoot.anchoredPosition = _contentFinalPosition + _contentStartOffset;
            }

            SetContentAlpha(0f);
            SetContentInteractable(false);
            RestoreGraphicColors();
        }

        private void SetDimAlpha(float alpha)
        {
            if (_dimImage == null)
            {
                return;
            }

            Color color = _dimColor;
            color.a = Clamp01(alpha);
            _dimImage.color = color;
            _dimImage.raycastTarget = true;
        }

        private void SetContentAlpha(float alpha)
        {
            if (_contentCanvasGroup == null)
            {
                return;
            }

            _contentCanvasGroup.alpha = Clamp01(alpha);
        }

        private void SetContentInteractable(bool interactable)
        {
            if (_contentCanvasGroup == null)
            {
                return;
            }

            bool allowInput = !_blockInputUntilContentShown || interactable;
            _contentCanvasGroup.interactable = allowInput;
            _contentCanvasGroup.blocksRaycasts = allowInput;
        }

        private Vector2 GetJitterOffset(float edge)
        {
            if (Time.unscaledTime < _nextJitterTime)
            {
                return _currentJitterOffset;
            }

            float offset = _glitchOffset * Clamp01(edge);
            _currentJitterOffset = new Vector2(
                Random.Range(-offset, offset),
                Random.Range(-offset * 0.25f, offset * 0.25f));

            float interval = _glitchInterval;
            if (interval < 0.005f)
            {
                interval = 0.005f;
            }

            _nextJitterTime = Time.unscaledTime + interval;
            return _currentJitterOffset;
        }

        private float GetFlickeredAlpha(float baseAlpha, float edge)
        {
            if (_flickerStrength <= 0f)
            {
                return baseAlpha;
            }

            float flickerLow = 1f - _flickerStrength;
            if (flickerLow < 0f)
            {
                flickerLow = 0f;
            }

            float flicker = Random.Range(flickerLow, 1f);
            return Clamp01(baseAlpha * Mathf.Lerp(1f, flicker, Clamp01(edge)));
        }

        private void ApplyFlash(float edge)
        {
            if (!_useFlash ||
                _contentGraphics == null ||
                _baseGraphicColors == null)
            {
                return;
            }

            float noise = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 70f));
            float amount = _flashStrength * Clamp01(edge) * noise;

            for (int i = 0; i < _contentGraphics.Length; i++)
            {
                Graphic graphic = _contentGraphics[i];
                if (graphic == null)
                {
                    continue;
                }

                Color baseColor = _baseGraphicColors[i];
                Color flashed = Color.Lerp(baseColor, _flashColor, amount);
                flashed.a = baseColor.a;
                graphic.color = flashed;
            }
        }

        private void RestoreGraphicColors()
        {
            if (_contentGraphics == null ||
                _baseGraphicColors == null)
            {
                return;
            }

            for (int i = 0; i < _contentGraphics.Length; i++)
            {
                if (_contentGraphics[i] != null)
                {
                    _contentGraphics[i].color = _baseGraphicColors[i];
                }
            }
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (_audioSource == null || clip == null)
            {
                return;
            }

            _audioSource.PlayOneShot(clip, Clamp01(volume));
        }

        private IEnumerator Wait(float seconds)
        {
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += GetDeltaTime();
                yield return null;
            }
        }

        private void StopRevealCoroutine()
        {
            if (_revealCoroutine == null)
            {
                return;
            }

            StopCoroutine(_revealCoroutine);
            _revealCoroutine = null;
        }

        private float GetDeltaTime()
        {
            return _useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }

        private static float EaseOutCubic(float value)
        {
            value = Clamp01(value);
            float inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseInOutCubic(float value)
        {
            value = Clamp01(value);

            if (value < 0.5f)
            {
                return 4f * value * value * value;
            }

            float f = -2f * value + 2f;
            return 1f - (f * f * f) * 0.5f;
        }
    }
}
