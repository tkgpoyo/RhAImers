using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    [DisallowMultipleComponent]
    public class RhymeFusionGenerationWaitEffect : MonoBehaviour
    {
        private sealed class WordItem
        {
            public RectTransform RectTransform;
            public TMP_Text Text;
            public CanvasGroup CanvasGroup;
            public Image PanelImage;
            public Vector2 LayoutPosition;
            public Vector2 FusionPosition;
            public Color BaseColor;
            public Color BasePanelColor;
        }

        [Header("References")]
        [SerializeField] private RectTransform _effectRoot;
        [SerializeField] private CanvasGroup _rootCanvasGroup;
        [SerializeField] private Image _wordPanelTemplate;
        [SerializeField] private TMP_Text _wordTemplate;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private bool _deactivateRootWhenHidden = false;

        [Header("Fusion Orb References")]
        [SerializeField] private RectTransform _fusionOrbRoot;
        [SerializeField] private CanvasGroup _fusionOrbCanvasGroup;
        [SerializeField] private Image _fusionOrbImage;
        [SerializeField] private Image _fusionRingImage;
        [SerializeField] private Image _fusionSecondRingImage;

        [Header("Sounds")]
        [SerializeField] private AudioClip _battlePanelsHiddenSound;
        [SerializeField] private AudioClip _wordAppearSound;
        [SerializeField] private AudioClip _fusionSound;
        [SerializeField] private float _soundVolume = 1f;

        [Header("Fusion Orb Waiting Sound")]
        [SerializeField] private AudioClip _fusionOrbWaitingSound;
        [SerializeField] private float _fusionOrbWaitingSoundIntervalSeconds = 0.85f;
        [SerializeField] private float _fusionOrbWaitingSoundVolume = 1f;
        [SerializeField] private bool _playFusionOrbWaitingSoundImmediately = true;

        [Header("Intro Timing")]
        [SerializeField] private float _introDelaySeconds = 0.24f;
        [SerializeField] private float _wordStartDelayAfterPanelHiddenSoundSeconds = 0.08f;

        [Header("Word Panel")]
        [SerializeField] private bool _useWordPanels = true;
        [SerializeField] private Sprite _wordPanelSprite;
        [SerializeField] private bool _useSlicedWordPanel = true;
        [SerializeField] private Vector2 _wordTextPadding = new Vector2(18f, 8f);
        [SerializeField] private Color _wordPanelLeftColor = new Color(0.32f, 0.02f, 0.28f, 0.82f);
        [SerializeField] private Color _wordPanelRightColor = new Color(0.02f, 0.18f, 0.32f, 0.82f);
        [SerializeField] private Color _wordPanelCenterColor = new Color(0.05f, 0.07f, 0.12f, 0.86f);

        [Header("Layout")]
        [SerializeField] private Vector2 _safeAreaPadding = new Vector2(170f, 120f);
        [SerializeField] private Vector2 _wordSize = new Vector2(230f, 64f);
        [SerializeField] private int _fixedLayoutLimit = 8;
        [SerializeField] private int _randomPlacementAttempts = 140;
        [SerializeField] private float _randomMinimumDistance = 145f;
        [SerializeField] private float _ellipseWidthRate = 0.62f;
        [SerializeField] private float _ellipseHeightRate = 0.62f;

        [Header("Colors")]
        [SerializeField] private Color _leftColor = new Color(1f, 0.18f, 0.88f, 1f);
        [SerializeField] private Color _rightColor = new Color(0.1f, 0.82f, 1f, 1f);
        [SerializeField] private Color _centerColor = Color.white;

        [Header("Fusion Orb Visual")]
        [SerializeField] private bool _useFusionOrb = true;
        [SerializeField] private bool _autoCreateFusionOrbVisuals = true;
        [SerializeField] private bool _hideWordsAfterFusion = true;
        [SerializeField] private Sprite _fusionOrbSprite;
        [SerializeField] private Sprite _fusionRingSprite;
        [SerializeField] private Vector2 _fusionOrbSize = new Vector2(220f, 220f);
        [SerializeField] private Vector2 _fusionRingSize = new Vector2(310f, 310f);
        [SerializeField] private Vector2 _fusionSecondRingSize = new Vector2(390f, 390f);
        [SerializeField] private Color _fusionOrbColor = new Color(0.92f, 0.18f, 1f, 0.95f);
        [SerializeField] private Color _fusionRingColor = new Color(0.12f, 0.86f, 1f, 0.82f);
        [SerializeField] private Color _fusionSecondRingColor = new Color(1f, 0.22f, 0.88f, 0.68f);
        [SerializeField] private float _fusionOrbRevealDurationSeconds = 0.28f;
        [SerializeField] private float _wordFadeOutDuringOrbSeconds = 0.18f;
        [SerializeField] private float _fusionOrbStartScale = 0.42f;
        [SerializeField] private float _fusionOrbReadyScale = 1f;

        [Header("Fusion Orb Motion")]
        [SerializeField] private float _fusionOrbPulseSpeed = 3.4f;
        [SerializeField] private float _fusionOrbPulseAmount = 0.07f;
        [SerializeField] private float _fusionRingPulseAmount = 0.035f;
        [SerializeField] private float _fusionRingRotationSpeed = 54f;
        [SerializeField] private float _fusionSecondRingRotationSpeed = -32f;

        [Header("Appear Animation")]
        [SerializeField] private float _appearIntervalSeconds = 0.16f;
        [SerializeField] private float _appearDurationSeconds = 0.28f;
        [SerializeField] private float _appearRiseDistance = 36f;
        [SerializeField] private float _appearStartScale = 0.88f;
        [SerializeField] private float _appearEndScale = 1f;

        [Header("Fusion Animation")]
        [SerializeField] private float _fusionStartDelaySeconds = 0.18f;
        [SerializeField] private float _fusionDurationSeconds = 0.62f;
        [SerializeField] private float _fusionEndScale = 0.72f;
        [SerializeField] private float _centerClusterRadius = 14f;

        [Header("Waiting Animation")]
        [SerializeField] private float _centerPulseSpeed = 3.4f;
        [SerializeField] private float _centerPulseAmount = 0.055f;
        [SerializeField] private float _centerOrbitAmount = 5f;
        [SerializeField] private float _hideDurationSeconds = 0.20f;

        private readonly List<WordItem> _items = new();
        private CancellationTokenSource _sequenceCts;
        private bool _hideRequested;
        private Sprite _generatedFusionOrbSprite;
        private Sprite _generatedFusionRingSprite;
        private float _nextFusionOrbWaitingSoundTime;
        private bool _isDisabling;

        public bool IsPlaying { get; private set; }
        public bool IsReadyForVerse { get; private set; }

        private void Awake()
        {
            ResolveReferences();
            SetTemplateVisible(false);
            PrepareFusionOrbVisuals();
            SetFusionOrbVisible(false, 0f);
            SetRootVisible(false, 0f);
        }

        private void OnDisable()
        {
            _isDisabling = true;

            try
            {
                CancelSequence();
                ClearWords();
                SetFusionOrbHiddenImmediateWithoutPreparing(deactivateObject: false);
                IsPlaying = false;
                IsReadyForVerse = false;
            }
            finally
            {
                _isDisabling = false;
            }
        }

        private void OnDestroy()
        {
            DestroyGeneratedSprite(ref _generatedFusionOrbSprite);
            DestroyGeneratedSprite(ref _generatedFusionRingSprite);
        }

        public void Begin(IReadOnlyList<string> rhymes, CancellationToken ownerToken = default)
        {
            StopImmediate();
            ResolveReferences();

            _hideRequested = false;
            _sequenceCts = CancellationTokenSource.CreateLinkedTokenSource(ownerToken);

            List<string> preparedRhymes = BuildRhymeList(rhymes);
            RunSequenceAsync(preparedRhymes, _sequenceCts.Token).Forget(HandleSequenceException);
        }

        public async UniTask WaitUntilReadyToShowVerseAsync(CancellationToken ct = default)
        {
            while (IsPlaying && !IsReadyForVerse)
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        public async UniTask HideAsync(CancellationToken ct = default)
        {
            if (!IsPlaying && (_rootCanvasGroup == null || _rootCanvasGroup.alpha <= 0f))
            {
                StopImmediate();
                return;
            }

            _hideRequested = true;
            ResolveReferences();

            float fromAlpha = _rootCanvasGroup != null ? _rootCanvasGroup.alpha : 1f;
            await FadeRootAsync(fromAlpha, 0f, _hideDurationSeconds, ct);

            StopImmediate();
        }

        public void StopImmediate()
        {
            _hideRequested = true;
            CancelSequence();
            ClearWords();
            SetTemplateVisible(false);
            SetFusionOrbVisible(false, 0f);
            SetRootVisible(false, 0f);
            IsPlaying = false;
            IsReadyForVerse = false;
        }

        private async UniTask RunSequenceAsync(List<string> rhymes, CancellationToken ct)
        {
            ResolveReferences();
            ClearWords();
            PrepareFusionOrbVisuals();
            SetFusionOrbVisible(false, 0f);
            SetRootVisible(true, 1f);

            IsPlaying = true;
            IsReadyForVerse = false;

            if (_introDelaySeconds > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_introDelaySeconds), ignoreTimeScale: true, cancellationToken: ct);
            }

            if (_hideRequested)
            {
                return;
            }

            PlayOneShot(_battlePanelsHiddenSound);

            if (_wordStartDelayAfterPanelHiddenSoundSeconds > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_wordStartDelayAfterPanelHiddenSoundSeconds), ignoreTimeScale: true, cancellationToken: ct);
            }

            if (_hideRequested)
            {
                return;
            }

            if (rhymes.Count == 0)
            {
                await ShowFusionOrbAsync(ct);
                IsReadyForVerse = true;
                await WaitAtCenterAsync(ct);
                return;
            }

            List<Vector2> layoutPositions = BuildLayoutPositions(rhymes.Count);

            for (int i = 0; i < rhymes.Count; i++)
            {
                WordItem item = CreateWordItem(rhymes[i], layoutPositions[i], i, rhymes.Count);
                _items.Add(item);
            }

            for (int i = 0; i < _items.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                if (_hideRequested)
                {
                    return;
                }

                PlayOneShot(_wordAppearSound);
                await AnimateWordAppearAsync(_items[i], ct);

                if (i < _items.Count - 1 && _appearIntervalSeconds > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_appearIntervalSeconds), ignoreTimeScale: true, cancellationToken: ct);
                }
            }

            if (_fusionStartDelaySeconds > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_fusionStartDelaySeconds), ignoreTimeScale: true, cancellationToken: ct);
            }

            if (_hideRequested)
            {
                return;
            }

            PlayOneShot(_fusionSound);
            await AnimateFusionAsync(ct);
            await ShowFusionOrbAsync(ct);

            IsReadyForVerse = true;
            await WaitAtCenterAsync(ct);
        }

        private async UniTask AnimateWordAppearAsync(WordItem item, CancellationToken ct)
        {
            if (item == null || item.RectTransform == null || item.CanvasGroup == null)
            {
                return;
            }

            Vector2 startPosition = item.LayoutPosition + Vector2.down * Mathf.Max(0f, _appearRiseDistance);
            Vector2 endPosition = item.LayoutPosition;
            float duration = Mathf.Max(0f, _appearDurationSeconds);

            item.RectTransform.anchoredPosition = startPosition;
            item.RectTransform.localScale = Vector3.one * Mathf.Max(0f, _appearStartScale);
            item.CanvasGroup.alpha = 0f;

            if (duration <= 0f)
            {
                item.RectTransform.anchoredPosition = endPosition;
                item.RectTransform.localScale = Vector3.one * Mathf.Max(0f, _appearEndScale);
                item.CanvasGroup.alpha = 1f;
                return;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                if (_hideRequested)
                {
                    return;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);

                item.RectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, eased);
                item.RectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(
                    Mathf.Max(0f, _appearStartScale),
                    Mathf.Max(0f, _appearEndScale),
                    EaseOutBack(t)
                );
                item.CanvasGroup.alpha = eased;

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            item.RectTransform.anchoredPosition = endPosition;
            item.RectTransform.localScale = Vector3.one * Mathf.Max(0f, _appearEndScale);
            item.CanvasGroup.alpha = 1f;
        }

        private async UniTask AnimateFusionAsync(CancellationToken ct)
        {
            float duration = Mathf.Max(0f, _fusionDurationSeconds);
            Vector2[] startPositions = new Vector2[_items.Count];
            Vector3[] startScales = new Vector3[_items.Count];
            Color[] startColors = new Color[_items.Count];
            Color[] startPanelColors = new Color[_items.Count];

            for (int i = 0; i < _items.Count; i++)
            {
                WordItem item = _items[i];

                if (item == null || item.RectTransform == null || item.Text == null)
                {
                    continue;
                }

                startPositions[i] = item.RectTransform.anchoredPosition;
                startScales[i] = item.RectTransform.localScale;
                startColors[i] = item.Text.color;
                startPanelColors[i] = item.PanelImage != null ? item.PanelImage.color : item.BasePanelColor;
                item.FusionPosition = GetCenterPosition() + GetClusterOffset(i, _items.Count);
            }

            if (duration <= 0f)
            {
                ApplyFusionVisual(1f, startPositions, startScales, startColors, startPanelColors);
                return;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                if (_hideRequested)
                {
                    return;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseInOutCubic(t);

                ApplyFusionVisual(eased, startPositions, startScales, startColors, startPanelColors);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            ApplyFusionVisual(1f, startPositions, startScales, startColors, startPanelColors);
        }

        private void ApplyFusionVisual(float t, Vector2[] startPositions, Vector3[] startScales, Color[] startColors, Color[] startPanelColors)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                WordItem item = _items[i];

                if (item == null || item.RectTransform == null || item.Text == null)
                {
                    continue;
                }

                item.RectTransform.anchoredPosition = Vector2.LerpUnclamped(startPositions[i], item.FusionPosition, t);
                item.RectTransform.localScale = Vector3.LerpUnclamped(startScales[i], Vector3.one * Mathf.Max(0f, _fusionEndScale), t);
                item.Text.color = Color.LerpUnclamped(startColors[i], _centerColor, t);

                if (item.PanelImage != null && startPanelColors != null && i < startPanelColors.Length)
                {
                    item.PanelImage.color = Color.LerpUnclamped(startPanelColors[i], _wordPanelCenterColor, t);
                }
            }
        }

        private async UniTask WaitAtCenterAsync(CancellationToken ct)
        {
            while (!_hideRequested)
            {
                ct.ThrowIfCancellationRequested();

                if (ShouldShowFusionOrb())
                {
                    UpdateFusionOrbWaitingSound();
                    UpdateFusionOrbMotion();
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    continue;
                }

                float pulse = 1f + Mathf.Sin(Time.unscaledTime * Mathf.Max(0f, _centerPulseSpeed)) * Mathf.Max(0f, _centerPulseAmount);
                Vector2 orbit = new Vector2(
                    Mathf.Cos(Time.unscaledTime * Mathf.Max(0f, _centerPulseSpeed)) * Mathf.Max(0f, _centerOrbitAmount),
                    Mathf.Sin(Time.unscaledTime * Mathf.Max(0f, _centerPulseSpeed) * 0.8f) * Mathf.Max(0f, _centerOrbitAmount) * 0.45f
                );

                for (int i = 0; i < _items.Count; i++)
                {
                    WordItem item = _items[i];

                    if (item == null || item.RectTransform == null)
                    {
                        continue;
                    }

                    item.RectTransform.anchoredPosition = item.FusionPosition + orbit * (i % 2 == 0 ? 1f : -1f);
                    item.RectTransform.localScale = Vector3.one * Mathf.Max(0f, _fusionEndScale) * pulse;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }

        private async UniTask ShowFusionOrbAsync(CancellationToken ct)
        {
            if (!_useFusionOrb)
            {
                return;
            }

            PrepareFusionOrbVisuals();

            if (_fusionOrbRoot == null || _fusionOrbCanvasGroup == null)
            {
                return;
            }

            float duration = Mathf.Max(0f, _fusionOrbRevealDurationSeconds);
            float wordFadeDuration = Mathf.Max(0f, _wordFadeOutDuringOrbSeconds);
            float[] startWordAlphas = new float[_items.Count];

            for (int i = 0; i < _items.Count; i++)
            {
                WordItem item = _items[i];
                startWordAlphas[i] = item != null && item.CanvasGroup != null
                    ? item.CanvasGroup.alpha
                    : 0f;
            }

            SetFusionOrbVisible(true, 0f);
            _fusionOrbRoot.anchoredPosition = GetCenterPosition();
            _fusionOrbRoot.localScale = Vector3.one * Mathf.Max(0f, _fusionOrbStartScale);
            ResetFusionOrbWaitingSoundSchedule();

            if (duration <= 0f)
            {
                SetFusionOrbVisible(true, 1f);
                _fusionOrbRoot.localScale = Vector3.one * Mathf.Max(0f, _fusionOrbReadyScale);
                ApplyWordFusionHide(1f, startWordAlphas);
                return;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                if (_hideRequested)
                {
                    return;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);

                _fusionOrbCanvasGroup.alpha = eased;
                _fusionOrbRoot.localScale = Vector3.one * Mathf.LerpUnclamped(
                    Mathf.Max(0f, _fusionOrbStartScale),
                    Mathf.Max(0f, _fusionOrbReadyScale),
                    EaseOutBack(t)
                );

                float wordT = wordFadeDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / wordFadeDuration);
                ApplyWordFusionHide(wordT, startWordAlphas);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            _fusionOrbCanvasGroup.alpha = 1f;
            _fusionOrbRoot.localScale = Vector3.one * Mathf.Max(0f, _fusionOrbReadyScale);
            ApplyWordFusionHide(1f, startWordAlphas);
        }

        private void ApplyWordFusionHide(float t, float[] startWordAlphas)
        {
            if (!_hideWordsAfterFusion)
            {
                return;
            }

            t = Mathf.Clamp01(t);

            for (int i = 0; i < _items.Count; i++)
            {
                WordItem item = _items[i];

                if (item == null || item.CanvasGroup == null)
                {
                    continue;
                }

                float startAlpha = startWordAlphas != null && i < startWordAlphas.Length
                    ? startWordAlphas[i]
                    : item.CanvasGroup.alpha;
                item.CanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, EaseOutCubic(t));

                if (t >= 1f && item.RectTransform != null)
                {
                    item.RectTransform.gameObject.SetActive(false);
                }
            }
        }

        private bool ShouldShowFusionOrb()
        {
            return _useFusionOrb
                && _fusionOrbCanvasGroup != null
                && _fusionOrbCanvasGroup.alpha > 0.01f;
        }

        private void UpdateFusionOrbMotion()
        {
            if (_fusionOrbRoot == null)
            {
                return;
            }

            float speed = Mathf.Max(0f, _fusionOrbPulseSpeed);
            float phase = Time.unscaledTime * speed;
            float pulse = 1f + Mathf.Sin(phase) * Mathf.Max(0f, _fusionOrbPulseAmount);

            _fusionOrbRoot.anchoredPosition = GetCenterPosition();
            _fusionOrbRoot.localScale = Vector3.one * Mathf.Max(0f, _fusionOrbReadyScale) * pulse;

            ApplyRingMotion(_fusionRingImage, _fusionRingRotationSpeed, 1f, phase);
            ApplyRingMotion(_fusionSecondRingImage, _fusionSecondRingRotationSpeed, -1f, phase);
        }

        private void ApplyRingMotion(Image ringImage, float rotationSpeed, float pulseDirection, float phase)
        {
            if (ringImage == null)
            {
                return;
            }

            RectTransform ringRect = ringImage.rectTransform;
            float ringPulse = 1f + Mathf.Sin(phase * 1.23f) * Mathf.Max(0f, _fusionRingPulseAmount) * pulseDirection;
            ringRect.localScale = Vector3.one * Mathf.Max(0.01f, ringPulse);
            ringRect.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * rotationSpeed);
        }

        private async UniTask FadeRootAsync(float fromAlpha, float toAlpha, float duration, CancellationToken ct)
        {
            ResolveReferences();

            if (_rootCanvasGroup == null)
            {
                return;
            }

            duration = Mathf.Max(0f, duration);

            if (duration <= 0f)
            {
                _rootCanvasGroup.alpha = toAlpha;
                return;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _rootCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, EaseInOutCubic(t));

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            _rootCanvasGroup.alpha = toAlpha;
        }

        private WordItem CreateWordItem(string word, Vector2 position, int index, int count)
        {
            RectTransform rectTransform = CreateWordRoot(index, out Image panelImage);
            TMP_Text text = CreateTextInstance(index, rectTransform);
            CanvasGroup canvasGroup = rectTransform.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = rectTransform.gameObject.AddComponent<CanvasGroup>();
            }

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            if (_wordSize.x > 0f && _wordSize.y > 0f)
            {
                rectTransform.sizeDelta = _wordSize;
            }

            text.text = word;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.Center;

            Color baseColor = GetColorForPosition(position);
            Color basePanelColor = GetPanelColorForPosition(position);
            text.color = baseColor;

            if (panelImage != null)
            {
                PreparePanelImage(panelImage, basePanelColor);
            }

            canvasGroup.alpha = 0f;
            rectTransform.anchoredPosition = position + Vector2.down * Mathf.Max(0f, _appearRiseDistance);
            rectTransform.localScale = Vector3.one * Mathf.Max(0f, _appearStartScale);
            rectTransform.gameObject.SetActive(true);

            return new WordItem
            {
                RectTransform = rectTransform,
                Text = text,
                CanvasGroup = canvasGroup,
                PanelImage = panelImage,
                LayoutPosition = position,
                FusionPosition = GetCenterPosition() + GetClusterOffset(index, count),
                BaseColor = baseColor,
                BasePanelColor = basePanelColor
            };
        }

        private RectTransform CreateWordRoot(int index, out Image panelImage)
        {
            panelImage = null;

            if (_useWordPanels && _wordPanelTemplate != null)
            {
                Image panel = Instantiate(_wordPanelTemplate, _effectRoot);
                panel.name = $"RhymeWaitWord_{index + 1}";
                panelImage = panel;
                return panel.rectTransform;
            }

            Type[] components = _useWordPanels
                ? new[] { typeof(RectTransform), typeof(CanvasRenderer), typeof(Image) }
                : new[] { typeof(RectTransform) };

            GameObject rootObject = new GameObject($"RhymeWaitWord_{index + 1}", components);
            rootObject.transform.SetParent(_effectRoot, false);

            if (_useWordPanels)
            {
                panelImage = rootObject.GetComponent<Image>();
            }

            return rootObject.GetComponent<RectTransform>();
        }

        private TMP_Text CreateTextInstance(int index, Transform parent)
        {
            TMP_Text text;

            if (_wordTemplate != null)
            {
                text = Instantiate(_wordTemplate, parent);
                text.name = $"RhymeWaitWordText_{index + 1}";
                PrepareTextRect(text.rectTransform);
                text.gameObject.SetActive(true);
                return text;
            }

            GameObject textObject = new GameObject($"RhymeWaitWordText_{index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            text = textObject.GetComponent<TMP_Text>();
            text.fontSize = 34f;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            PrepareTextRect(text.rectTransform);
            return text;
        }

        private void PrepareTextRect(RectTransform textRect)
        {
            if (textRect == null)
            {
                return;
            }

            Vector2 padding = new Vector2(
                Mathf.Max(0f, _wordTextPadding.x),
                Mathf.Max(0f, _wordTextPadding.y)
            );

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = padding;
            textRect.offsetMax = -padding;
            textRect.localScale = Vector3.one;
        }

        private void PreparePanelImage(Image panelImage, Color color)
        {
            if (panelImage == null)
            {
                return;
            }

            panelImage.raycastTarget = false;
            panelImage.color = color;

            if (_wordPanelSprite != null)
            {
                panelImage.sprite = _wordPanelSprite;
            }

            panelImage.type = _useSlicedWordPanel && panelImage.sprite != null
                ? Image.Type.Sliced
                : Image.Type.Simple;
        }

        private List<string> BuildRhymeList(IReadOnlyList<string> rhymes)
        {
            var list = new List<string>();

            if (rhymes == null)
            {
                return list;
            }

            for (int i = 0; i < rhymes.Count; i++)
            {
                string word = rhymes[i];

                if (string.IsNullOrWhiteSpace(word))
                {
                    continue;
                }

                list.Add(word.Trim());
            }

            return list;
        }

        private List<Vector2> BuildLayoutPositions(int count)
        {
            var positions = new List<Vector2>(count);
            Vector2 halfSize = GetUsableHalfSize();

            if (count <= 0)
            {
                return positions;
            }

            if (count == 1)
            {
                positions.Add(GetCenterPosition());
                return positions;
            }

            if (count == 2)
            {
                positions.Add(GetCenterPosition() + new Vector2(-halfSize.x * 0.42f, 0f));
                positions.Add(GetCenterPosition() + new Vector2(halfSize.x * 0.42f, 0f));
                return positions;
            }

            if (count == 3)
            {
                positions.Add(GetCenterPosition() + new Vector2(0f, halfSize.y * 0.36f));
                positions.Add(GetCenterPosition() + new Vector2(-halfSize.x * 0.42f, -halfSize.y * 0.30f));
                positions.Add(GetCenterPosition() + new Vector2(halfSize.x * 0.42f, -halfSize.y * 0.30f));
                return positions;
            }

            if (count == 4)
            {
                positions.Add(GetCenterPosition() + new Vector2(-halfSize.x * 0.38f, halfSize.y * 0.30f));
                positions.Add(GetCenterPosition() + new Vector2(halfSize.x * 0.38f, halfSize.y * 0.30f));
                positions.Add(GetCenterPosition() + new Vector2(-halfSize.x * 0.38f, -halfSize.y * 0.30f));
                positions.Add(GetCenterPosition() + new Vector2(halfSize.x * 0.38f, -halfSize.y * 0.30f));
                return positions;
            }

            if (count <= Mathf.Max(4, _fixedLayoutLimit))
            {
                for (int i = 0; i < count; i++)
                {
                    positions.Add(GetCenterPosition() + GetEllipsePosition(i, count, halfSize));
                }

                return positions;
            }

            return BuildRandomPositions(count, halfSize);
        }

        private List<Vector2> BuildRandomPositions(int count, Vector2 halfSize)
        {
            var positions = new List<Vector2>(count);
            float baseDistance = Mathf.Max(20f, _randomMinimumDistance);
            int attempts = Mathf.Max(8, _randomPlacementAttempts);

            for (int i = 0; i < count; i++)
            {
                bool placed = false;
                float currentDistance = baseDistance;

                for (int relaxation = 0; relaxation < 7 && !placed; relaxation++)
                {
                    for (int attempt = 0; attempt < attempts; attempt++)
                    {
                        Vector2 candidate = GetCenterPosition() + new Vector2(
                            UnityEngine.Random.Range(-halfSize.x, halfSize.x),
                            UnityEngine.Random.Range(-halfSize.y, halfSize.y)
                        );

                        if (HasEnoughDistance(candidate, positions, currentDistance))
                        {
                            positions.Add(candidate);
                            placed = true;
                            break;
                        }
                    }

                    currentDistance *= 0.86f;
                }

                if (!placed)
                {
                    Vector2 fallback = GetCenterPosition() + GetEllipsePosition(i, count, halfSize) * UnityEngine.Random.Range(0.55f, 1f);
                    positions.Add(fallback);
                }
            }

            return positions;
        }

        private bool HasEnoughDistance(Vector2 candidate, List<Vector2> positions, float distance)
        {
            float sqrDistance = distance * distance;

            for (int i = 0; i < positions.Count; i++)
            {
                if ((candidate - positions[i]).sqrMagnitude < sqrDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private Vector2 GetEllipsePosition(int index, int count, Vector2 halfSize)
        {
            float angle = Mathf.PI * 0.5f - Mathf.PI * 2f * index / Mathf.Max(1, count);
            return new Vector2(
                Mathf.Cos(angle) * halfSize.x * Mathf.Max(0f, _ellipseWidthRate),
                Mathf.Sin(angle) * halfSize.y * Mathf.Max(0f, _ellipseHeightRate)
            );
        }

        private Vector2 GetClusterOffset(int index, int count)
        {
            float radius = Mathf.Max(0f, _centerClusterRadius);

            if (radius <= 0f || count <= 1)
            {
                return Vector2.zero;
            }

            float angle = Mathf.PI * 2f * index / count;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private Vector2 GetCenterPosition()
        {
            return Vector2.zero;
        }

        private Vector2 GetUsableHalfSize()
        {
            ResolveReferences();

            float width = Screen.width;
            float height = Screen.height;

            if (_effectRoot != null)
            {
                Rect rect = _effectRoot.rect;

                if (rect.width > 1f)
                {
                    width = rect.width;
                }

                if (rect.height > 1f)
                {
                    height = rect.height;
                }
            }

            return new Vector2(
                Mathf.Max(40f, width * 0.5f - Mathf.Max(0f, _safeAreaPadding.x)),
                Mathf.Max(40f, height * 0.5f - Mathf.Max(0f, _safeAreaPadding.y))
            );
        }

        private Color GetColorForPosition(Vector2 position)
        {
            Vector2 halfSize = GetUsableHalfSize();
            float t = Mathf.InverseLerp(-halfSize.x, halfSize.x, position.x);
            return Color.Lerp(_leftColor, _rightColor, t);
        }

        private Color GetPanelColorForPosition(Vector2 position)
        {
            Vector2 halfSize = GetUsableHalfSize();
            float t = Mathf.InverseLerp(-halfSize.x, halfSize.x, position.x);
            return Color.Lerp(_wordPanelLeftColor, _wordPanelRightColor, t);
        }

        private void ResolveReferences()
        {
            if (_effectRoot == null)
            {
                _effectRoot = transform as RectTransform;
            }

            if (_rootCanvasGroup == null && _effectRoot != null)
            {
                _rootCanvasGroup = _effectRoot.GetComponent<CanvasGroup>();
            }

            if (_rootCanvasGroup == null && _effectRoot != null)
            {
                _rootCanvasGroup = _effectRoot.gameObject.AddComponent<CanvasGroup>();
            }

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
        }

        private void PrepareFusionOrbVisuals()
        {
            if (!_useFusionOrb)
            {
                return;
            }

            if (_effectRoot == null)
            {
                _effectRoot = transform as RectTransform;
            }

            if (_fusionOrbRoot == null && _fusionOrbImage != null)
            {
                _fusionOrbRoot = _fusionOrbImage.transform.parent as RectTransform;

                if (_fusionOrbRoot == null)
                {
                    _fusionOrbRoot = _fusionOrbImage.rectTransform;
                }
            }

            if (_fusionOrbRoot == null && _autoCreateFusionOrbVisuals && _effectRoot != null)
            {
                GameObject rootObject = new GameObject(
                    "FusionOrbVisual",
                    typeof(RectTransform),
                    typeof(CanvasGroup)
                );
                rootObject.transform.SetParent(_effectRoot, false);
                _fusionOrbRoot = rootObject.GetComponent<RectTransform>();
                _fusionOrbCanvasGroup = rootObject.GetComponent<CanvasGroup>();
            }

            if (_fusionOrbRoot == null)
            {
                return;
            }

            _fusionOrbRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _fusionOrbRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _fusionOrbRoot.pivot = new Vector2(0.5f, 0.5f);
            _fusionOrbRoot.anchoredPosition = GetCenterPosition();
            _fusionOrbRoot.sizeDelta = MaxVector(_fusionSecondRingSize, _fusionRingSize, _fusionOrbSize);

            if (CanChangeFusionOrbSibling())
            {
                _fusionOrbRoot.SetAsLastSibling();
            }

            if (_fusionOrbCanvasGroup == null)
            {
                _fusionOrbCanvasGroup = _fusionOrbRoot.GetComponent<CanvasGroup>();
            }

            if (_fusionOrbCanvasGroup == null)
            {
                _fusionOrbCanvasGroup = _fusionOrbRoot.gameObject.AddComponent<CanvasGroup>();
            }

            if (_fusionOrbImage == null && _autoCreateFusionOrbVisuals)
            {
                _fusionOrbImage = CreateFusionImage("FusionOrb", _fusionOrbRoot);
            }

            if (_fusionRingImage == null && _autoCreateFusionOrbVisuals)
            {
                _fusionRingImage = CreateFusionImage("FusionRing", _fusionOrbRoot);
            }

            if (_fusionSecondRingImage == null && _autoCreateFusionOrbVisuals)
            {
                _fusionSecondRingImage = CreateFusionImage("FusionSecondRing", _fusionOrbRoot);
            }

            PrepareFusionImage(_fusionOrbImage, GetFusionOrbSprite(), _fusionOrbColor, _fusionOrbSize);
            PrepareFusionImage(_fusionRingImage, GetFusionRingSprite(), _fusionRingColor, _fusionRingSize);
            PrepareFusionImage(_fusionSecondRingImage, GetFusionRingSprite(), _fusionSecondRingColor, _fusionSecondRingSize);
        }

        private Image CreateFusionImage(string objectName, Transform parent)
        {
            GameObject imageObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            imageObject.transform.SetParent(parent, false);
            return imageObject.GetComponent<Image>();
        }

        private void PrepareFusionImage(Image image, Sprite sprite, Color color, Vector2 size)
        {
            if (image == null)
            {
                return;
            }

            image.raycastTarget = false;
            image.sprite = sprite;
            image.color = color;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;

            RectTransform rectTransform = image.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;

            if (size.x > 0f && size.y > 0f)
            {
                rectTransform.sizeDelta = size;
            }
        }

        private void SetFusionOrbVisible(bool visible, float alpha)
        {
            if (!_useFusionOrb)
            {
                return;
            }

            if (_isDisabling)
            {
                SetFusionOrbHiddenImmediateWithoutPreparing(deactivateObject: false);
                return;
            }

            PrepareFusionOrbVisuals();

            if (_fusionOrbRoot != null && _fusionOrbRoot.gameObject.activeSelf != visible)
            {
                _fusionOrbRoot.gameObject.SetActive(visible);
            }

            if (_fusionOrbCanvasGroup != null)
            {
                _fusionOrbCanvasGroup.alpha = alpha;
                _fusionOrbCanvasGroup.interactable = false;
                _fusionOrbCanvasGroup.blocksRaycasts = false;
            }
        }

        private bool CanChangeFusionOrbSibling()
        {
            if (_isDisabling || _fusionOrbRoot == null)
            {
                return false;
            }

            if (!gameObject.activeInHierarchy)
            {
                return false;
            }

            if (_effectRoot != null && !_effectRoot.gameObject.activeInHierarchy)
            {
                return false;
            }

            return true;
        }

        private void SetFusionOrbHiddenImmediateWithoutPreparing(bool deactivateObject)
        {
            if (_fusionOrbCanvasGroup != null)
            {
                _fusionOrbCanvasGroup.alpha = 0f;
                _fusionOrbCanvasGroup.interactable = false;
                _fusionOrbCanvasGroup.blocksRaycasts = false;
            }

            if (deactivateObject && _fusionOrbRoot != null && _fusionOrbRoot.gameObject.activeSelf)
            {
                _fusionOrbRoot.gameObject.SetActive(false);
            }
        }

        private Sprite GetFusionOrbSprite()
        {
            if (_fusionOrbSprite != null)
            {
                return _fusionOrbSprite;
            }

            if (_generatedFusionOrbSprite == null)
            {
                _generatedFusionOrbSprite = CreateGeneratedOrbSprite("GeneratedFusionOrbSprite", 160);
            }

            return _generatedFusionOrbSprite;
        }

        private Sprite GetFusionRingSprite()
        {
            if (_fusionRingSprite != null)
            {
                return _fusionRingSprite;
            }

            if (_generatedFusionRingSprite == null)
            {
                _generatedFusionRingSprite = CreateGeneratedRingSprite("GeneratedFusionRingSprite", 192);
            }

            return _generatedFusionRingSprite;
        }

        private Sprite CreateGeneratedOrbSprite(string spriteName, int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = spriteName + "_Texture";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = ((x + 0.5f) / size - 0.5f) * 2f;
                    float dy = ((y + 0.5f) / size - 0.5f) * 2f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    float core = 1f - Mathf.InverseLerp(0.04f, 0.35f, distance);
                    float glow = 1f - Mathf.InverseLerp(0.18f, 1f, distance);
                    float alpha = Mathf.Clamp01(core * 0.75f + Mathf.Pow(Mathf.Clamp01(glow), 2.2f) * 0.65f);

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateGeneratedRingSprite(string spriteName, int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = spriteName + "_Texture";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = ((x + 0.5f) / size - 0.5f) * 2f;
                    float dy = ((y + 0.5f) / size - 0.5f) * 2f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    float outerRing = 1f - Mathf.Clamp01(Mathf.Abs(distance - 0.78f) / 0.035f);
                    float innerRing = 1f - Mathf.Clamp01(Mathf.Abs(distance - 0.56f) / 0.018f);
                    float segment = Mathf.Lerp(0.45f, 1f, Mathf.Abs(Mathf.Sin(angle * 8f)));
                    float alpha = Mathf.Clamp01(outerRing * segment + innerRing * 0.45f);

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private Vector2 MaxVector(Vector2 a, Vector2 b, Vector2 c)
        {
            return new Vector2(
                Mathf.Max(a.x, Mathf.Max(b.x, c.x)),
                Mathf.Max(a.y, Mathf.Max(b.y, c.y))
            );
        }

        private void DestroyGeneratedSprite(ref Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Texture2D texture = sprite.texture;
            Destroy(sprite);

            if (texture != null)
            {
                Destroy(texture);
            }

            sprite = null;
        }

        private void SetRootVisible(bool visible, float alpha)
        {
            ResolveReferences();

            GameObject rootObject = _effectRoot != null ? _effectRoot.gameObject : gameObject;
            bool shouldBeActive = visible || !_deactivateRootWhenHidden;

            if (rootObject != null && rootObject.activeSelf != shouldBeActive)
            {
                rootObject.SetActive(shouldBeActive);
            }

            if (_rootCanvasGroup != null)
            {
                _rootCanvasGroup.alpha = alpha;
                _rootCanvasGroup.interactable = false;
                _rootCanvasGroup.blocksRaycasts = false;
            }
        }

        private void SetTemplateVisible(bool visible)
        {
            if (_wordPanelTemplate != null && _wordPanelTemplate.gameObject.activeSelf != visible)
            {
                _wordPanelTemplate.gameObject.SetActive(visible);
            }

            if (_wordTemplate != null && _wordTemplate.gameObject.activeSelf != visible)
            {
                _wordTemplate.gameObject.SetActive(visible);
            }
        }

        private void ClearWords()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                WordItem item = _items[i];

                if (item != null && item.RectTransform != null)
                {
                    Destroy(item.RectTransform.gameObject);
                }
            }

            _items.Clear();
        }

        private void CancelSequence()
        {
            if (_sequenceCts == null)
            {
                return;
            }

            _sequenceCts.Cancel();
            _sequenceCts.Dispose();
            _sequenceCts = null;
        }

        private void PlayOneShot(AudioClip clip)
        {
            PlayOneShot(clip, _soundVolume);
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (_audioSource == null || clip == null)
            {
                return;
            }

            _audioSource.PlayOneShot(clip, Mathf.Max(0f, volume));
        }

        private void ResetFusionOrbWaitingSoundSchedule()
        {
            if (_fusionOrbWaitingSound == null)
            {
                _nextFusionOrbWaitingSoundTime = 0f;
                return;
            }

            float interval = Mathf.Max(0.01f, _fusionOrbWaitingSoundIntervalSeconds);
            _nextFusionOrbWaitingSoundTime = Time.unscaledTime + (_playFusionOrbWaitingSoundImmediately ? 0f : interval);
        }

        private void UpdateFusionOrbWaitingSound()
        {
            if (_fusionOrbWaitingSound == null || _audioSource == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < _nextFusionOrbWaitingSoundTime)
            {
                return;
            }

            PlayOneShot(_fusionOrbWaitingSound, _fusionOrbWaitingSoundVolume);

            float interval = Mathf.Max(0.01f, _fusionOrbWaitingSoundIntervalSeconds);
            _nextFusionOrbWaitingSoundTime = now + interval;
        }

        private void HandleSequenceException(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                IsPlaying = false;
                IsReadyForVerse = false;
                return;
            }

            Debug.LogException(exception, this);
            StopImmediate();
        }

        private float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private float EaseInOutCubic(float t)
        {
            t = Mathf.Clamp01(t);

            if (t < 0.5f)
            {
                return 4f * t * t * t;
            }

            float f = -2f * t + 2f;
            return 1f - f * f * f * 0.5f;
        }

        private float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
