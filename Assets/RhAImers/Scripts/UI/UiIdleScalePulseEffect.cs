using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RhAImers.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/UI Idle Scale Pulse Effect")]
    public sealed class UiIdleScalePulseEffect : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [Header("Target")]
        [SerializeField] private RectTransform _scaleTarget;
        [SerializeField] private global::UiButtonHoverScaleSoundEffect _hoverScaleEffect;
        [SerializeField] private CanvasGroup _visibilityCanvasGroup;

        [Header("Pulse")]
        [SerializeField] private bool _playOnEnable = true;
        [SerializeField] private float _baseScaleMultiplier = 1f;
        [SerializeField] private float _pulseAmount = 0.025f;
        [SerializeField] private float _cyclesPerSecond = 0.75f;
        [SerializeField] private float _phaseOffset;
        [SerializeField] private bool _randomizePhaseOnEnable = true;
        [SerializeField] private bool _useUnscaledTime = true;

        [Header("Behavior")]
        [SerializeField] private bool _driveHoverScaleEffectWhenAvailable = true;
        [SerializeField] private bool _captureRestScaleOnEnable = true;
        [SerializeField] private bool _restoreOnDisable = true;
        [SerializeField] private bool _pauseWhenInvisible;
        [SerializeField] private bool _pauseOnPointerHover = true;
        [SerializeField] private bool _pauseOnKeyboardSelect;

        private Vector3 _restScale = Vector3.one;
        private float _restHoverBaseMultiplier = 1f;
        private float _elapsed;
        private bool _hasRestScale;
        private bool _hasRestHoverBaseMultiplier;
        private bool _playing;
        private bool _isPointerInside;
        private bool _isSelected;

        private void Reset()
        {
            ResolveReferences();
            CaptureRestState();
        }

        private void Awake()
        {
            ResolveReferences();
            CaptureRestState();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_captureRestScaleOnEnable || !_hasRestScale || !_hasRestHoverBaseMultiplier)
            {
                CaptureRestState();
            }

            if (_randomizePhaseOnEnable)
            {
                _phaseOffset = Random.value;
            }

            _isPointerInside = false;
            _isSelected = false;
            _playing = _playOnEnable;
            ApplyPulse();
        }

        private void OnDisable()
        {
            if (!_restoreOnDisable)
            {
                return;
            }

            _isPointerInside = false;
            _isSelected = false;

            if (_driveHoverScaleEffectWhenAvailable && _hoverScaleEffect != null && _hasRestHoverBaseMultiplier)
            {
                _hoverScaleEffect.BaseScaleMultiplier = _restHoverBaseMultiplier;
                return;
            }

            if (_scaleTarget != null && _hasRestScale)
            {
                _scaleTarget.localScale = _restScale;
            }
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (!_playing)
            {
                return;
            }

            if (_pauseWhenInvisible && _visibilityCanvasGroup != null && _visibilityCanvasGroup.alpha <= 0.001f)
            {
                ApplyBase();
                return;
            }

            if (ShouldPauseForInteraction())
            {
                ApplyBase();
                return;
            }

            _elapsed += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            ApplyPulse();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerInside = true;

            if (_pauseOnPointerHover)
            {
                ApplyBase();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerInside = false;

            if (_playing)
            {
                ApplyPulse();
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            _isSelected = true;

            if (_pauseOnKeyboardSelect)
            {
                ApplyBase();
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _isSelected = false;

            if (_playing)
            {
                ApplyPulse();
            }
        }

        public void Play()
        {
            _playing = true;
        }

        public void Stop()
        {
            _playing = false;
            ApplyBase();
        }

        [ContextMenu("Capture Current Scale As Rest")]
        public void CaptureCurrentScaleAsRest()
        {
            CaptureRestState();
        }

        private void ResolveReferences()
        {
            if (_scaleTarget == null)
            {
                _scaleTarget = transform as RectTransform;
            }

            if (_hoverScaleEffect == null)
            {
                _hoverScaleEffect = GetComponent<global::UiButtonHoverScaleSoundEffect>();
            }

            if (_visibilityCanvasGroup == null)
            {
                _visibilityCanvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void CaptureRestState()
        {
            if (_scaleTarget != null)
            {
                _restScale = _scaleTarget.localScale;
                _hasRestScale = true;
            }

            if (_hoverScaleEffect != null)
            {
                _restHoverBaseMultiplier = _hoverScaleEffect.BaseScaleMultiplier;
                _hasRestHoverBaseMultiplier = true;
            }
        }

        private void ApplyPulse()
        {
            float pulse = CalculatePulseMultiplier();
            ApplyMultiplier(pulse);
        }

        private void ApplyBase()
        {
            ApplyMultiplier(1f);
        }

        private bool ShouldPauseForInteraction()
        {
            if (_pauseOnPointerHover && _isPointerInside)
            {
                return true;
            }

            if (_pauseOnKeyboardSelect && _isSelected)
            {
                return true;
            }

            return false;
        }

        private float CalculatePulseMultiplier()
        {
            float amount = Mathf.Max(0f, _pulseAmount);
            float speed = Mathf.Max(0f, _cyclesPerSecond);
            float wave = Mathf.Sin((_elapsed * speed + _phaseOffset) * Mathf.PI * 2f);
            float easedWave = (wave + 1f) * 0.5f;
            easedWave = Mathf.SmoothStep(0f, 1f, easedWave);
            return _baseScaleMultiplier * (1f + (easedWave * 2f - 1f) * amount);
        }

        private void ApplyMultiplier(float multiplier)
        {
            if (_driveHoverScaleEffectWhenAvailable && _hoverScaleEffect != null)
            {
                float baseMultiplier = _hasRestHoverBaseMultiplier ? _restHoverBaseMultiplier : 1f;
                _hoverScaleEffect.BaseScaleMultiplier = baseMultiplier * multiplier;
                return;
            }

            if (_scaleTarget != null && _hasRestScale)
            {
                _scaleTarget.localScale = _restScale * multiplier;
            }
        }
    }
}
