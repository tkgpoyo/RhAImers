using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanelTransition : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _target;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Initial State")]
        [SerializeField] private bool _startHiddenOnAwake = true;

        [Header("Hologram Show")]
        [SerializeField] private float _showDuration = 0.35f;
        [SerializeField] private float _hideDuration = 0.20f;
        [SerializeField] private float _glitchOffset = 10f;
        [SerializeField] private float _glitchInterval = 0.035f;
        [SerializeField, Range(0f, 1f)] private float _flickerStrength = 0.28f;

        [Header("Flash")]
        [SerializeField] private bool _useFlash = true;
        [SerializeField] private Color _flashColor = new Color(0.2f, 1f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float _flashStrength = 0.45f;

        [Header("Visibility")]
        [SerializeField] private bool _hideObjectAfterHide = true;

        private Graphic[] _graphics;
        private Color[] _baseGraphicColors;
        private Vector2 _baseAnchoredPosition;
        private Vector3 _baseScale;

        private Coroutine _transitionCoroutine;
        private bool _initialized;
        private bool _targetVisible;
        private float _nextGlitchTime;
        private Vector2 _currentGlitchOffset;

        public bool IsTransitioning { get; private set; }

        public bool IsVisible
        {
            get
            {
                EnsureInitialized();
                return _targetVisible
                    && gameObject.activeInHierarchy
                    && _canvasGroup != null
                    && _canvasGroup.alpha >= 0.99f
                    && !IsTransitioning;
            }
        }

        private void Awake()
        {
            EnsureInitialized();

            if (_startHiddenOnAwake)
            {
                HideImmediate();
            }
        }

        private void OnDisable()
        {
            StopTransition();
            RestoreVisuals();
        }

        public void CaptureCurrentVisualState()
        {
            EnsureInitialized();

            _graphics = GetComponentsInChildren<Graphic>(true);
            _baseGraphicColors = new Color[_graphics.Length];

            for (int i = 0; i < _graphics.Length; i++)
            {
                _baseGraphicColors[i] = _graphics[i].color;
            }

            if (_target != null)
            {
                _baseAnchoredPosition = _target.anchoredPosition;
                _baseScale = _target.localScale;
            }

            RestoreVisuals();
        }

        public void Show()
        {
            EnsureInitialized();

            if (_targetVisible && gameObject.activeSelf && _canvasGroup.alpha >= 0.99f)
            {
                return;
            }

            if (_targetVisible && _transitionCoroutine != null)
            {
                return;
            }

            _targetVisible = true;
            gameObject.SetActive(true);

            StopTransition();
            _transitionCoroutine = StartCoroutine(ShowRoutine());
        }

        public void Hide()
        {
            EnsureInitialized();

            if (!_targetVisible && (!gameObject.activeSelf || _canvasGroup.alpha <= 0.01f))
            {
                if (_hideObjectAfterHide && gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            if (!_targetVisible && _transitionCoroutine != null)
            {
                return;
            }

            _targetVisible = false;

            if (!gameObject.activeSelf)
            {
                return;
            }

            StopTransition();
            _transitionCoroutine = StartCoroutine(HideRoutine());
        }

        public void ShowImmediate()
        {
            EnsureInitialized();

            StopTransition();
            IsTransitioning = false;
            gameObject.SetActive(true);
            _targetVisible = true;
            SetCanvasGroup(1f, true);
            RestoreVisuals();
        }

        public void HideImmediate()
        {
            EnsureInitialized();

            StopTransition();
            IsTransitioning = false;
            _targetVisible = false;
            SetCanvasGroup(0f, false);
            RestoreVisuals();

            if (_hideObjectAfterHide && gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator ShowRoutine()
        {
            IsTransitioning = true;
            SetCanvasGroup(0f, false);
            RestoreVisuals();

            float elapsed = 0f;
            _nextGlitchTime = 0f;
            _currentGlitchOffset = Vector2.zero;

            while (elapsed < _showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, _showDuration));
                float eased = EaseOutCubic(t);

                float flicker = GetFlickerMultiplier(t);
                _canvasGroup.alpha = Mathf.Clamp01(eased * flicker);

                ApplyHologramJitter(t, true);
                ApplyFlash(t, true);

                yield return null;
            }

            _canvasGroup.alpha = 1f;
            SetCanvasGroup(1f, true);
            RestoreVisuals();
            IsTransitioning = false;
            _transitionCoroutine = null;
        }

        private IEnumerator HideRoutine()
        {
            IsTransitioning = true;
            SetCanvasGroup(_canvasGroup.alpha, false);

            float elapsed = 0f;
            _nextGlitchTime = 0f;
            _currentGlitchOffset = Vector2.zero;

            while (elapsed < _hideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, _hideDuration));
                float eased = EaseInCubic(t);

                float flicker = GetFlickerMultiplier(t);
                _canvasGroup.alpha = Mathf.Clamp01((1f - eased) * flicker);

                ApplyHologramJitter(t, false);
                ApplyFlash(t, false);

                yield return null;
            }

            SetCanvasGroup(0f, false);
            RestoreVisuals();

            if (_hideObjectAfterHide)
            {
                gameObject.SetActive(false);
            }

            IsTransitioning = false;
            _transitionCoroutine = null;
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            if (_target == null)
            {
                _target = transform as RectTransform;
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            _graphics = GetComponentsInChildren<Graphic>(true);
            _baseGraphicColors = new Color[_graphics.Length];

            for (int i = 0; i < _graphics.Length; i++)
            {
                _baseGraphicColors[i] = _graphics[i].color;
            }

            if (_target != null)
            {
                _baseAnchoredPosition = _target.anchoredPosition;
                _baseScale = _target.localScale;
            }

            _targetVisible = gameObject.activeSelf && _canvasGroup != null && _canvasGroup.alpha > 0.01f;
            _initialized = true;
        }

        private void StopTransition()
        {
            if (_transitionCoroutine == null)
            {
                return;
            }

            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
            IsTransitioning = false;
        }

        private void SetCanvasGroup(float alpha, bool interactive)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = alpha;
            _canvasGroup.interactable = interactive;
            _canvasGroup.blocksRaycasts = interactive;
        }

        private void ApplyHologramJitter(float t, bool showing)
        {
            if (_target == null)
            {
                return;
            }

            if (Time.unscaledTime >= _nextGlitchTime)
            {
                float strength = showing ? (1f - t) : t;
                float offset = _glitchOffset * Mathf.Clamp01(strength);
                _currentGlitchOffset = new Vector2(
                    Random.Range(-offset, offset),
                    Random.Range(-offset * 0.25f, offset * 0.25f)
                );

                _nextGlitchTime = Time.unscaledTime + Mathf.Max(0.005f, _glitchInterval);
            }

            float scalePulse = 1f + Mathf.Sin(Time.unscaledTime * 80f) * 0.006f * Mathf.Clamp01(showing ? (1f - t) : t);

            _target.anchoredPosition = _baseAnchoredPosition + _currentGlitchOffset;
            _target.localScale = _baseScale * scalePulse;
        }

        private void ApplyFlash(float t, bool showing)
        {
            if (!_useFlash || _graphics == null || _baseGraphicColors == null)
            {
                return;
            }

            float edge = showing ? 1f - Mathf.Clamp01(t / 0.45f) : Mathf.Clamp01(t);
            float noise = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 70f));
            float amount = _flashStrength * edge * noise;

            for (int i = 0; i < _graphics.Length; i++)
            {
                if (_graphics[i] == null)
                {
                    continue;
                }

                Color baseColor = _baseGraphicColors[i];
                Color flashed = Color.Lerp(baseColor, _flashColor, amount);
                flashed.a = baseColor.a;
                _graphics[i].color = flashed;
            }
        }

        private float GetFlickerMultiplier(float t)
        {
            float edgeStrength = 1f - Mathf.Clamp01(t);
            float randomFlicker = Random.Range(1f - _flickerStrength, 1f);
            return Mathf.Lerp(1f, randomFlicker, edgeStrength);
        }

        private void RestoreVisuals()
        {
            if (_target != null)
            {
                _target.anchoredPosition = _baseAnchoredPosition;
                _target.localScale = _baseScale;
            }

            if (_graphics == null || _baseGraphicColors == null)
            {
                return;
            }

            for (int i = 0; i < _graphics.Length; i++)
            {
                if (_graphics[i] != null)
                {
                    _graphics[i].color = _baseGraphicColors[i];
                }
            }
        }

        private float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private float EaseInCubic(float t)
        {
            return t * t * t;
        }
    }
}
