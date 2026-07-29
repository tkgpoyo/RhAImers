using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Result Score Reveal Sequence")]
    public sealed class ResultScoreRevealSequence : MonoBehaviour
    {
        [Header("Playback")]
        [SerializeField] private bool _playOnStart = true;
        [SerializeField] private bool _useUnscaledTime = true;
        [SerializeField] private int _waitFramesBeforeReveal = 1;

        [Header("Scene Fade Wait")]
        [SerializeField] private bool _waitForSceneFadeOverlay = true;
        [SerializeField] private float _delayAfterFade = 1.25f;
        [SerializeField] private float _sceneFadeWaitTimeout = 5f;

        [Header("Score Texts")]
        [SerializeField] private TurnScoreRevealTargets[] _turns = new TurnScoreRevealTargets[3];
        [SerializeField] private TextMeshProUGUI _totalScoreText;

        [Header("Button")]
        [SerializeField] private bool _hideButtonUntilEnd = true;
        [SerializeField] private CanvasGroup _buttonCanvasGroup;
        [SerializeField] private Selectable _buttonSelectable;
        [SerializeField] private float _buttonRevealDuration = 0.25f;

        [Header("Timing")]
        [SerializeField] private float _initialDelay = 0.35f;
        [SerializeField] private float _turnStartDelay = 0.12f;
        [SerializeField] private float _betweenItemDelay = 0.22f;
        [SerializeField] private float _afterTurnTotalDelay = 0.34f;
        [SerializeField] private float _beforeTotalDelay = 0.45f;

        [Header("Stamp Motion")]
        [SerializeField] private float _itemStampDuration = 0.18f;
        [SerializeField] private float _turnTotalStampDuration = 0.24f;
        [SerializeField] private float _finalTotalStampDuration = 0.34f;
        [SerializeField] private float _itemStartScale = 1.32f;
        [SerializeField] private float _turnTotalStartScale = 1.45f;
        [SerializeField] private float _finalTotalStartScale = 1.65f;
        [SerializeField] private float _stampCompressionScale = 0.96f;

        [Header("Flash")]
        [SerializeField] private bool _useTextFlash = true;
        [SerializeField] private Color _itemFlashColor = new Color(0.25f, 0.95f, 1f, 1f);
        [SerializeField] private Color _turnTotalFlashColor = new Color(1f, 0.25f, 0.95f, 1f);
        [SerializeField] private Color _finalTotalFlashColor = new Color(1f, 1f, 1f, 1f);

        [Header("Turn Panel Pulse")]
        [SerializeField] private bool _pulseTurnPanel = true;
        [SerializeField] private Color _panelPulseColor = new Color(0.2f, 0.9f, 1f, 1f);
        [SerializeField] private float _panelPulseDuration = 0.2f;
        [SerializeField] private float _panelPulseStrength = 0.32f;

        [Header("Count Up")]
        [SerializeField] private bool _countUpTurnTotals = false;
        [SerializeField] private bool _countUpFinalTotal = false;
        [SerializeField] private float _turnTotalCountDuration = 0.35f;
        [SerializeField] private float _finalTotalCountDuration = 0.85f;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _itemStampClip;
        [SerializeField] private AudioClip _turnTotalStampClip;
        [SerializeField] private AudioClip _finalTotalStartClip;
        [SerializeField] private AudioClip _finalTotalStampClip;
        [SerializeField] private AudioClip _buttonRevealClip;
        [SerializeField] private float _itemVolume = 0.75f;
        [SerializeField] private float _turnTotalVolume = 0.9f;
        [SerializeField] private float _finalTotalStartVolume = 0.65f;
        [SerializeField] private float _finalTotalStampVolume = 1f;
        [SerializeField] private float _buttonRevealVolume = 0.75f;

        [Header("BGM After Reveal")]
        [SerializeField] private bool _playBgmAfterReveal = true;
        [SerializeField] private global::SceneBgmPlayer _sceneBgmPlayerToPlay;
        [SerializeField] private bool _autoFindSceneBgmPlayerToPlay = true;

        private readonly Dictionary<TextMeshProUGUI, TextVisualState> _textStates = new Dictionary<TextMeshProUGUI, TextVisualState>();
        private readonly Dictionary<Graphic, Color> _graphicColors = new Dictionary<Graphic, Color>();
        private Coroutine _sequenceCoroutine;
        private Vector3 _buttonBaseScale = Vector3.one;
        private bool _hasButtonBaseScale;

        private void Awake()
        {
            ResolveReferences();
            CaptureBaseVisuals();
            ApplyHiddenState();
        }

        private void Start()
        {
            if (_playOnStart)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            StopSequence();
            RestoreBaseVisuals();
        }

        [ContextMenu("Play")]
        public void Play()
        {
            StopSequence();
            ResolveReferences();
            CaptureBaseVisuals();
            ApplyHiddenState();
            _sequenceCoroutine = StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            for (int i = 0; i < _waitFramesBeforeReveal; i++)
            {
                yield return null;
            }

            CaptureCurrentTextValues();
            ApplyHiddenState();

            yield return WaitForSceneFadeOverlayIfNeeded();

            if (_initialDelay > 0f)
            {
                yield return Wait(_initialDelay);
            }

            int turnCount = _turns != null ? _turns.Length : 0;

            for (int turnIndex = 0; turnIndex < turnCount; turnIndex++)
            {
                TurnScoreRevealTargets turn = _turns[turnIndex];

                if (turn == null)
                {
                    continue;
                }

                if (_pulseTurnPanel)
                {
                    StartCoroutine(PulsePanelRoutine(turn.PanelGraphic));
                }

                if (_turnStartDelay > 0f)
                {
                    yield return Wait(_turnStartDelay);
                }

                yield return RevealStampRoutine(turn.RhymeCountText, _itemStampDuration, _itemStartScale, _itemFlashColor, _itemStampClip, _itemVolume, false, 0f);
                yield return Wait(_betweenItemDelay);

                yield return RevealStampRoutine(turn.AverageHardnessText, _itemStampDuration, _itemStartScale, _itemFlashColor, _itemStampClip, _itemVolume, false, 0f);
                yield return Wait(_betweenItemDelay);

                yield return RevealStampRoutine(turn.RelevanceCountText, _itemStampDuration, _itemStartScale, _itemFlashColor, _itemStampClip, _itemVolume, false, 0f);
                yield return Wait(_betweenItemDelay);

                yield return RevealStampRoutine(turn.TotalText, _turnTotalStampDuration, _turnTotalStartScale, _turnTotalFlashColor, _turnTotalStampClip, _turnTotalVolume, _countUpTurnTotals, _turnTotalCountDuration);

                if (_afterTurnTotalDelay > 0f)
                {
                    yield return Wait(_afterTurnTotalDelay);
                }
            }

            if (_beforeTotalDelay > 0f)
            {
                yield return Wait(_beforeTotalDelay);
            }

            if (_countUpFinalTotal)
            {
                PlayOneShot(_finalTotalStartClip, _finalTotalStartVolume);
            }

            yield return RevealStampRoutine(_totalScoreText, _finalTotalStampDuration, _finalTotalStartScale, _finalTotalFlashColor, _finalTotalStampClip, _finalTotalStampVolume, _countUpFinalTotal, _finalTotalCountDuration);

            PlayBgmAfterRevealIfNeeded();

            if (_hideButtonUntilEnd)
            {
                PlayOneShot(_buttonRevealClip, _buttonRevealVolume);
                yield return RevealButtonRoutine();
            }

            _sequenceCoroutine = null;
        }

        private IEnumerator RevealStampRoutine(
            TextMeshProUGUI text,
            float stampDuration,
            float startScale,
            Color flashColor,
            AudioClip clip,
            float volume,
            bool countUp,
            float countDuration)
        {
            if (text == null)
            {
                yield break;
            }

            TextVisualState state = GetTextState(text);
            string finalValue = state.Value;

            int finalInt = 0;
            bool canCount = countUp && TryReadInt(finalValue, out finalInt);
            float duration = canCount && countDuration > stampDuration
                ? countDuration
                : stampDuration;

            if (duration <= 0f)
            {
                text.text = finalValue;
                ApplyTextAlpha(text, state.BaseColor, 1f);
                text.rectTransform.localScale = state.BaseScale;
                yield break;
            }

            PlayOneShot(clip, volume);

            float elapsed = 0f;
            bool finishedCount = !canCount;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Clamp01(elapsed / duration);

                if (canCount)
                {
                    int current = RoundToInt(finalInt * EaseOutCubic(t));
                    text.text = current.ToString();

                    if (t >= 1f && !finishedCount)
                    {
                        text.text = finalValue;
                        finishedCount = true;
                    }
                }
                else
                {
                    text.text = finalValue;
                }

                float stampT = stampDuration <= 0f
                    ? 1f
                    : Clamp01(elapsed / stampDuration);

                float alpha = EaseOutCubic(stampT);
                float scale = CalculateStampScale(startScale, _stampCompressionScale, stampT);

                ApplyTextAlpha(text, state.BaseColor, alpha);
                ApplyTextFlash(text, state.BaseColor, flashColor, stampT);
                text.rectTransform.localScale = state.BaseScale * scale;

                yield return null;
            }

            text.text = finalValue;
            ApplyTextAlpha(text, state.BaseColor, 1f);
            text.rectTransform.localScale = state.BaseScale;
        }

        private IEnumerator PulsePanelRoutine(Graphic panelGraphic)
        {
            if (panelGraphic == null || _panelPulseDuration <= 0f)
            {
                yield break;
            }

            Color baseColor = GetGraphicBaseColor(panelGraphic);
            float elapsed = 0f;

            while (elapsed < _panelPulseDuration)
            {
                elapsed += GetDeltaTime();
                float t = Clamp01(elapsed / _panelPulseDuration);
                float pulse = Mathf.Sin(t * Mathf.PI) * Clamp01(_panelPulseStrength);
                Color color = Color.Lerp(baseColor, _panelPulseColor, pulse);
                color.a = baseColor.a;
                panelGraphic.color = color;
                yield return null;
            }

            panelGraphic.color = baseColor;
        }

        private IEnumerator RevealButtonRoutine()
        {
            if (_buttonCanvasGroup == null)
            {
                yield break;
            }

            if (_buttonRevealDuration <= 0f)
            {
                SetButtonVisible(true);
                yield break;
            }

            SetButtonInput(false);

            float elapsed = 0f;

            while (elapsed < _buttonRevealDuration)
            {
                elapsed += GetDeltaTime();
                float t = Clamp01(elapsed / _buttonRevealDuration);
                float eased = EaseOutCubic(t);

                _buttonCanvasGroup.alpha = eased;

                if (_buttonCanvasGroup.transform is RectTransform rectTransform)
                {
                    float scale = CalculateStampScale(1.18f, 0.98f, t);
                    rectTransform.localScale = _buttonBaseScale * scale;
                }

                yield return null;
            }

            SetButtonVisible(true);
        }

        private void ResolveReferences()
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
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f;

            if (_buttonCanvasGroup == null && _buttonSelectable != null)
            {
                _buttonCanvasGroup = _buttonSelectable.GetComponent<CanvasGroup>();
            }

            if (_buttonCanvasGroup == null && _buttonSelectable != null)
            {
                _buttonCanvasGroup = _buttonSelectable.gameObject.AddComponent<CanvasGroup>();
            }

            if (_buttonSelectable == null && _buttonCanvasGroup != null)
            {
                _buttonSelectable = _buttonCanvasGroup.GetComponent<Selectable>();
            }

            if (_sceneBgmPlayerToPlay == null && _autoFindSceneBgmPlayerToPlay)
            {
                _sceneBgmPlayerToPlay = FindFirstObjectByType<global::SceneBgmPlayer>();
            }
        }

        private void CaptureBaseVisuals()
        {
            foreach (TextMeshProUGUI text in EnumerateScoreTexts())
            {
                if (text == null)
                {
                    continue;
                }

                if (!_textStates.ContainsKey(text))
                {
                    _textStates.Add(text, new TextVisualState(text.text, text.color, text.rectTransform.localScale));
                }
            }

            if (_turns != null)
            {
                foreach (TurnScoreRevealTargets turn in _turns)
                {
                    if (turn == null || turn.PanelGraphic == null)
                    {
                        continue;
                    }

                    if (!_graphicColors.ContainsKey(turn.PanelGraphic))
                    {
                        _graphicColors.Add(turn.PanelGraphic, turn.PanelGraphic.color);
                    }
                }
            }

            if (_buttonCanvasGroup != null && !_hasButtonBaseScale)
            {
                _buttonBaseScale = _buttonCanvasGroup.transform.localScale;
                _hasButtonBaseScale = true;
            }
        }

        private void CaptureCurrentTextValues()
        {
            foreach (TextMeshProUGUI text in EnumerateScoreTexts())
            {
                if (text == null)
                {
                    continue;
                }

                TextVisualState state = GetTextState(text);
                state.Value = text.text;
                _textStates[text] = state;
            }
        }

        private void ApplyHiddenState()
        {
            foreach (TextMeshProUGUI text in EnumerateScoreTexts())
            {
                if (text == null)
                {
                    continue;
                }

                TextVisualState state = GetTextState(text);
                ApplyTextAlpha(text, state.BaseColor, 0f);
                text.rectTransform.localScale = state.BaseScale;
            }

            if (_hideButtonUntilEnd)
            {
                SetButtonVisible(false);
            }
        }

        private void RestoreBaseVisuals()
        {
            foreach (KeyValuePair<TextMeshProUGUI, TextVisualState> pair in _textStates)
            {
                if (pair.Key == null)
                {
                    continue;
                }

                pair.Key.color = pair.Value.BaseColor;
                pair.Key.rectTransform.localScale = pair.Value.BaseScale;
            }

            foreach (KeyValuePair<Graphic, Color> pair in _graphicColors)
            {
                if (pair.Key != null)
                {
                    pair.Key.color = pair.Value;
                }
            }

            if (_buttonCanvasGroup != null && _hasButtonBaseScale)
            {
                _buttonCanvasGroup.transform.localScale = _buttonBaseScale;
            }
        }

        private IEnumerable<TextMeshProUGUI> EnumerateScoreTexts()
        {
            if (_turns != null)
            {
                for (int i = 0; i < _turns.Length; i++)
                {
                    TurnScoreRevealTargets turn = _turns[i];

                    if (turn == null)
                    {
                        continue;
                    }

                    yield return turn.RhymeCountText;
                    yield return turn.AverageHardnessText;
                    yield return turn.RelevanceCountText;
                    yield return turn.TotalText;
                }
            }

            yield return _totalScoreText;
        }

        private TextVisualState GetTextState(TextMeshProUGUI text)
        {
            if (text != null && _textStates.TryGetValue(text, out TextVisualState state))
            {
                return state;
            }

            return new TextVisualState(text != null ? text.text : string.Empty, text != null ? text.color : Color.white, text != null ? text.rectTransform.localScale : Vector3.one);
        }

        private Color GetGraphicBaseColor(Graphic graphic)
        {
            if (graphic != null && _graphicColors.TryGetValue(graphic, out Color color))
            {
                return color;
            }

            return graphic != null ? graphic.color : Color.white;
        }

        private void SetButtonVisible(bool visible)
        {
            if (_buttonCanvasGroup != null)
            {
                _buttonCanvasGroup.alpha = visible ? 1f : 0f;
            }

            if (_buttonCanvasGroup != null && _hasButtonBaseScale)
            {
                _buttonCanvasGroup.transform.localScale = _buttonBaseScale;
            }

            SetButtonInput(visible);
        }

        private void SetButtonInput(bool enabled)
        {
            if (_buttonCanvasGroup != null)
            {
                _buttonCanvasGroup.interactable = enabled;
                _buttonCanvasGroup.blocksRaycasts = enabled;
            }

            if (_buttonSelectable != null)
            {
                _buttonSelectable.interactable = enabled;
            }
        }

        private void ApplyTextAlpha(TextMeshProUGUI text, Color baseColor, float alpha)
        {
            if (text == null)
            {
                return;
            }

            Color color = text.color;
            color.r = baseColor.r;
            color.g = baseColor.g;
            color.b = baseColor.b;
            color.a = baseColor.a * Clamp01(alpha);
            text.color = color;
        }

        private void ApplyTextFlash(TextMeshProUGUI text, Color baseColor, Color flashColor, float progress)
        {
            if (!_useTextFlash || text == null)
            {
                return;
            }

            float amount = 1f - Clamp01(progress);
            amount *= amount;
            Color color = Color.Lerp(baseColor, flashColor, amount);
            color.a = text.color.a;
            text.color = color;
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (_audioSource == null || clip == null)
            {
                return;
            }

            _audioSource.PlayOneShot(clip, Clamp01(volume));
        }

        private void PlayBgmAfterRevealIfNeeded()
        {
            if (!_playBgmAfterReveal)
            {
                return;
            }

            if (_sceneBgmPlayerToPlay == null && _autoFindSceneBgmPlayerToPlay)
            {
                _sceneBgmPlayerToPlay = FindFirstObjectByType<global::SceneBgmPlayer>();
            }

            if (_sceneBgmPlayerToPlay != null)
            {
                _sceneBgmPlayerToPlay.Play();
            }
        }

        private IEnumerator WaitForSceneFadeOverlayIfNeeded()
        {
            if (_waitForSceneFadeOverlay)
            {
                float elapsed = 0f;

                while (global::SceneFadeOverlay.IsTransitionActiveOrVisible)
                {
                    elapsed += GetDeltaTime();

                    if (_sceneFadeWaitTimeout > 0f && elapsed >= _sceneFadeWaitTimeout)
                    {
                        break;
                    }

                    yield return null;
                }
            }

            if (_delayAfterFade > 0f)
            {
                yield return Wait(_delayAfterFade);
            }
        }

        private void StopSequence()
        {
            if (_sequenceCoroutine == null)
            {
                return;
            }

            StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = null;
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

        private float GetDeltaTime()
        {
            return _useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;
        }

        private static float CalculateStampScale(float startScale, float compressionScale, float progress)
        {
            float t = Clamp01(progress);

            if (t < 0.72f)
            {
                float phase = t / 0.72f;
                return Lerp(startScale, compressionScale, EaseOutCubic(phase));
            }

            float settle = (t - 0.72f) / 0.28f;
            return Lerp(compressionScale, 1f, EaseOutCubic(settle));
        }

        private static float EaseOutCubic(float value)
        {
            float t = Clamp01(value);
            float inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
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

        private static float Lerp(float from, float to, float t)
        {
            return from + (to - from) * Clamp01(t);
        }

        private static int RoundToInt(float value)
        {
            if (value >= 0f)
            {
                return (int)(value + 0.5f);
            }

            return (int)(value - 0.5f);
        }

        private static bool TryReadInt(string text, out int value)
        {
            value = 0;

            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            string trimmed = text.Trim();
            bool negative = false;
            int index = 0;

            if (trimmed.Length > 0 && trimmed[0] == '-')
            {
                negative = true;
                index = 1;
            }

            bool foundDigit = false;
            int result = 0;

            for (; index < trimmed.Length; index++)
            {
                char c = trimmed[index];

                if (c < '0' || c > '9')
                {
                    continue;
                }

                foundDigit = true;
                result = result * 10 + (c - '0');
            }

            if (!foundDigit)
            {
                return false;
            }

            value = negative ? -result : result;
            return true;
        }

        private struct TextVisualState
        {
            public string Value;
            public readonly Color BaseColor;
            public readonly Vector3 BaseScale;

            public TextVisualState(string value, Color baseColor, Vector3 baseScale)
            {
                Value = value;
                BaseColor = baseColor;
                BaseScale = baseScale;
            }
        }
    }

    [Serializable]
    public sealed class TurnScoreRevealTargets
    {
        public RectTransform PanelRoot;
        public Graphic PanelGraphic;
        public TextMeshProUGUI RhymeCountText;
        public TextMeshProUGUI AverageHardnessText;
        public TextMeshProUGUI RelevanceCountText;
        public TextMeshProUGUI TotalText;
    }
}
