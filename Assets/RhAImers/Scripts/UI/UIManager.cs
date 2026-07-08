using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using RhAImers.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    public enum BattleUiPanelKind
    {
        OpponentVerse,
        PlayerVerse,
        RhymeInput
    }

    public class UIManager : MonoBehaviour
    {
        private const string HighlightColor = "#FFD54F";

        /// <summary>
        /// バトル画面内でどのグループを表示するかを表す，UIManager内部だけの表示状態．
        /// GameManagerのGameStateとは別物（ゲーム進行の状態管理はGameManager側の責務）．
        /// </summary>
        private enum BattlePhase
        {
            Hidden,
            Input,
            PlayerVerse
        }

        private struct VerseLineSegment
        {
            public int StartIndex;
            public int Length;

            public VerseLineSegment(int startIndex, int length)
            {
                StartIndex = startIndex;
                Length = length;
            }
        }

        private struct VerseLineVisualState
        {
            public Text Text;
            public CanvasGroup CanvasGroup;
            public Color FinalColor;
            public Vector3 FinalScale;
            public float FinalCanvasGroupAlpha;

            public VerseLineVisualState(Text text, CanvasGroup canvasGroup, Color finalColor, Vector3 finalScale, float finalCanvasGroupAlpha)
            {
                Text = text;
                CanvasGroup = canvasGroup;
                FinalColor = finalColor;
                FinalScale = finalScale;
                FinalCanvasGroupAlpha = finalCanvasGroupAlpha;
            }
        }

        [Header("Background UI")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Sprite _battleBackgroundSprite;
        [SerializeField] private Color _backgroundColor = Color.white;

        [Header("Battle Start Signal")]
        [SerializeField] private GameObject _battleStartSignalGroup;
        [SerializeField] private Image _battleStartSignalImage;
        [SerializeField] private CanvasGroup _battleStartSignalCanvasGroup;
        [SerializeField] private RectTransform _battleStartSignalRectTransform;
        [SerializeField] private float _battleStartSignalSlideInDuration = 0.45f;
        [SerializeField] private float _battleStartSignalHoldDuration = 0.65f;
        [SerializeField] private float _battleStartSignalSlideOutDuration = 0.40f;
        [SerializeField] private float _battleStartSignalPostDelay = 0.25f;
        [SerializeField] private float _battleStartSignalEnterOffsetX = 1600f;
        [SerializeField] private float _battleStartSignalExitOffsetX = -1600f;

        [Header("Battle Flow Visibility")]
        [SerializeField] private GameObject _opponentVerseGroup;
        [SerializeField] private GameObject _playerVerseGroup;
        [SerializeField] private GameObject _rhymeInputGroup;

        [Header("Battle Flow Transitions")]
        [SerializeField] private UIPanelTransition _opponentVerseTransition;
        [SerializeField] private UIPanelTransition _playerVerseTransition;
        [SerializeField] private UIPanelTransition _rhymeInputTransition;

        [Header("Verse Panel UI")]
        [SerializeField] private Image _opponentVersePanelImage;
        [SerializeField] private Image _playerVersePanelImage;
        [SerializeField] private Sprite _opponentVersePanelSprite;
        [SerializeField] private Sprite _playerVersePanelSprite;
        [SerializeField] private Color _opponentVersePanelColor = Color.white;
        [SerializeField] private Color _playerVersePanelColor = Color.white;
        [SerializeField] private bool _useSlicedVersePanels = true;

        [Header("Rhyme Input Panel UI")]
        [SerializeField] private Image _rhymeInputPanelImage;
        [SerializeField] private Sprite _rhymeInputPanelSprite;
        [SerializeField] private Color _rhymeInputPanelColor = Color.white;
        [SerializeField] private bool _useSlicedRhymeInputPanel = true;

        [Header("Rhyme Tab UI")]
        [SerializeField] private ScrollRect _inputRhymesScrollRect;
        [SerializeField] private RectTransform _inputRhymesContent;
        [SerializeField] private GameObject _inputRhymeTabTemplate;
        [SerializeField] private Text _inputRhymesEmptyText;
        [SerializeField] private string _rhymeTabWordTextName = "WordText";
        [SerializeField] private string _rhymeTabRemoveButtonName = "RemoveButton";

        [Header("Rhyme Tab Wrap Layout")]
        [SerializeField] private bool _wrapRhymeTabsIntoRows = true;
        [SerializeField, Min(1)] private int _rhymeTabsPerRow = 3;
        [SerializeField] private float _rhymeTabHorizontalSpacing = 8f;
        [SerializeField] private bool _centerRhymeTabWordText = true;
        [SerializeField] private float _inputRhymesScrollSensitivity = 120f;

        [Header("Rhyme Tab Dynamic Width")]
        [SerializeField] private bool _useDynamicRhymeTabWidth = true;
        [SerializeField] private float _rhymeTabMinWidth = 72f;
        [SerializeField] private float _rhymeTabMaxWidth = 220f;
        [SerializeField] private float _rhymeTabTextHorizontalPadding = 44f;
        [SerializeField] private float _rhymeTabFallbackRowWidth = 420f;
        [SerializeField] private bool _shrinkRhymeTextWhenOverflow = true;
        [SerializeField, Min(1)] private int _rhymeTabMinFontSize = 12;

        [Header("Battle UI")]
        [SerializeField] private Text _opponentVerseText;
        [SerializeField] private Text _inputTimerText;
        [SerializeField] private Text _inputRhymesText;
        [SerializeField] private Text _generatedVerseText;
        [SerializeField] private Text _resultText;
        [SerializeField] private Text _statusText;

        [Header("Result UI")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private Button _retryButton;

        [Header("Verse Line Presentation")]
        [SerializeField] private bool _showVerseLineByLine = true;
        [SerializeField] private float _verseLineIntervalSec = 0.12f;
        [SerializeField] private bool _clearVerseBeforeLinePresentation = true;

        [Header("Verse Line Animation")]
        [SerializeField] private bool _animateVerseLineAppearance = true;
        [SerializeField] private float _verseLineFadeInDurationSec = 0.10f;
        [SerializeField, Range(0f, 1f)] private float _verseLineStartAlpha = 0.35f;
        [SerializeField] private float _verseLineStartScale = 1.06f;

        [Header("Verse Presentation Timing")]
        [SerializeField] private bool _pauseGameTimeDuringVerseLinePresentation = true;

        [Header("Verse Line Objects")]
        [SerializeField] private RectTransform _opponentVerseLinesRoot;
        [SerializeField] private Text _opponentVerseLineTemplate;
        [SerializeField] private RectTransform _playerVerseLinesRoot;
        [SerializeField] private Text _playerVerseLineTemplate;
        [SerializeField] private bool _autoBuildVerseLineObjects = true;
        [SerializeField] private bool _hideFallbackVerseTextWhenUsingLineObjects = true;
        [SerializeField] private bool _applyVerseLineRootLayout = true;
        [SerializeField] private float _verseLineRootSpacing = 4f;
        [SerializeField] private TextAnchor _verseLineRootChildAlignment = TextAnchor.MiddleCenter;

        private readonly List<GameObject> _inputRhymeTabInstances = new();
        private readonly List<GameObject> _inputRhymeRowInstances = new();
        private readonly List<GameObject> _opponentVerseLineInstances = new();
        private readonly List<GameObject> _playerVerseLineInstances = new();

        private BattlePhase _currentPhase = BattlePhase.Hidden;
        private bool _isBattleStartSignalPlaying;
        private CancellationTokenSource _battleStartSignalCts;
        private CancellationTokenSource _verseLinePresentationCts;
        private Vector2 _battleStartSignalCenterPosition;
        private Text _activeVerseLineText;
        private CanvasGroup _activeVerseLineCanvasGroup;
        private Color _activeVerseLineOriginalColor;
        private Vector3 _activeVerseLineOriginalScale;
        private float _activeVerseLineOriginalCanvasGroupAlpha;
        private bool _isVersePresentationGameTimePaused;
        private float _versePresentationPreviousTimeScale = 1f;

        public event Action RetrySelected;
        public event Action<int> InputRhymeRemoveAtRequested;
        public event Action BattleStartSignalShown;
        public event Action<BattleUiPanelKind> BattlePanelShown;
        public event Action<BattleUiPanelKind> BattleVerseLineShown;
        public event Action BattleResultShown;

        private void Awake()
        {
            ApplyBattleBackground();
            ApplyVersePanels();
            ApplyRhymeInputPanel();
            PrepareBattleStartSignal();
            PrepareRhymeTabTemplate();
            PrepareVerseLineObjectTemplates();
            ResolvePanelTransitions();
            CapturePanelTransitionVisualStates();

            ApplyPhaseVisibility(BattlePhase.Hidden, animate: false);
            SetResultVisible(false);
        }

        private void OnEnable()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.AddListener(HandleRetryButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveListener(HandleRetryButtonClicked);
            }

            CancelBattleStartSignal();
            CancelVerseLinePresentation();
        }

        /// <summary>
        /// バトル開始演出（合図の表示・スライドイン→ホールド→スライドアウト）を再生します．
        /// 演出が完了する（またはキャンセルされる）まで待機します．
        /// </summary>
        public async UniTask ShowBattleStartSignalAsync()
        {
            CancelBattleStartSignal();
            _battleStartSignalCts = new CancellationTokenSource();
            var ct = _battleStartSignalCts.Token;

            if (!HasBattleStartSignal())
            {
                return;
            }

            SetResultVisible(false);

            try
            {
                _isBattleStartSignalPlaying = true;
                ApplyPhaseVisibility(BattlePhase.Hidden, animate: false);
                SetBattleStartSignalVisible(true, 1f);
                _battleStartSignalRectTransform.anchoredPosition = GetBattleStartSignalEnterPosition();
                BattleStartSignalShown?.Invoke();

                await MoveBattleStartSignalAsync(
                    GetBattleStartSignalEnterPosition(),
                    _battleStartSignalCenterPosition,
                    _battleStartSignalSlideInDuration,
                    useEaseOut: true,
                    ct
                );

                if (_battleStartSignalHoldDuration > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_battleStartSignalHoldDuration),
                        ignoreTimeScale: true,
                        cancellationToken: ct
                    );
                }

                await MoveBattleStartSignalAsync(
                    _battleStartSignalCenterPosition,
                    GetBattleStartSignalExitPosition(),
                    _battleStartSignalSlideOutDuration,
                    useEaseOut: false,
                    ct
                );

                SetBattleStartSignalVisible(false, 0f);
                _battleStartSignalRectTransform.anchoredPosition = _battleStartSignalCenterPosition;

                if (_battleStartSignalPostDelay > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_battleStartSignalPostDelay),
                        ignoreTimeScale: true,
                        cancellationToken: ct
                    );
                }
            }
            catch (OperationCanceledException)
            {
                // 演出が中断された場合は何もしない
            }
            finally
            {
                _isBattleStartSignalPlaying = false;
            }
        }

        public void ShowTitle()
        {
            CancelVerseLinePresentation();
            SetStatus("Title");
            SetResultVisible(false);
        }

        public void ShowModeSelect()
        {
            CancelVerseLinePresentation();
            SetStatus("Mode Select");
            SetResultVisible(false);
        }

        public void ShowOpponentVerse(Verse verse)
        {
            ShowInputPhase();
            SetStatus("Opponent Verse");
            SetResultVisible(false);

            StartVerseLinePresentation(_opponentVerseText, verse, BattleUiPanelKind.OpponentVerse);
        }

        public void ShowInputTimer(int sec)
        {
            ShowInputPhase();
            UpdateInputTimer(sec);
        }

        public void UpdateInputTimer(int sec)
        {
            SetText(_inputTimerText, FormatTimer(sec));
        }

        public void ShowInputRhymes(IReadOnlyList<string> rhymes)
        {
            ShowInputPhase();
            UpdateInputRhymesTextFallback(rhymes);
            RebuildInputRhymeTabs(rhymes);
        }

        public void ShowGeneratedVerse(Verse verse)
        {
            ShowPlayerVersePhase();
            SetStatus("Generated Verse");
            SetResultVisible(false);

            StartVerseLinePresentation(_generatedVerseText, verse, BattleUiPanelKind.PlayerVerse);
        }

        public void ShowResult(BattleResult result)
        {
            CancelVerseLinePresentation();
            HideAllVerseLineObjectPresentations();
            HideBattlePhaseGroups();
            SetStatus("Result");
            SetResultVisible(true);
            BattleResultShown?.Invoke();

            string resultText = BuildResultText(result);
            SetText(_resultText, resultText);
        }

        public void ShowOpponentVerseLoading()
        {
            CancelVerseLinePresentation();
            SetVerseLineObjectPresentationVisible(BattleUiPanelKind.OpponentVerse, false);
            SetVerseFallbackTextVisible(_opponentVerseText, true);
            ShowInputPhase();
            SetStatus("Opponent Verse Loading");
            SetResultVisible(false);
            SetText(_opponentVerseText, "相手のバース生成中...");
        }

        public void ShowGenerationLoading()
        {
            CancelVerseLinePresentation();
            SetVerseLineObjectPresentationVisible(BattleUiPanelKind.PlayerVerse, false);
            SetVerseFallbackTextVisible(_generatedVerseText, true);
            ShowPlayerVersePhase();
            SetStatus("Verse Generation Loading");
            SetResultVisible(false);
            SetText(_generatedVerseText, "あなたのバース生成中...");
        }

        public void ShowScoringLoading()
        {
            CancelVerseLinePresentation();
            HideAllVerseLineObjectPresentations();
            HideBattlePhaseGroups();
            SetStatus("Scoring Loading");
            SetResultVisible(false);
            SetText(_resultText, "採点中...");
        }

        public void ShowInputPhase()
        {
            ApplyPhaseVisibility(BattlePhase.Input, animate: true);
        }

        public void ShowPlayerVersePhase()
        {
            ApplyPhaseVisibility(BattlePhase.PlayerVerse, animate: true);
        }

        public void HideBattlePhaseGroups()
        {
            CancelVerseLinePresentation();
            HideAllVerseLineObjectPresentations();
            ApplyPhaseVisibility(BattlePhase.Hidden, animate: true);
        }

        public void HideBattlePhaseGroupsImmediate()
        {
            CancelVerseLinePresentation();
            HideAllVerseLineObjectPresentations();
            ApplyPhaseVisibility(BattlePhase.Hidden, animate: false);
        }

        private void ApplyPhaseVisibility(BattlePhase phase, bool animate)
        {
            if (animate && _currentPhase == phase)
            {
                return;
            }

            bool isEnteringInputPhase = phase == BattlePhase.Input && _currentPhase != BattlePhase.Input;

            switch (phase)
            {
                case BattlePhase.Input:
                    if (isEnteringInputPhase)
                    {
                        ResetInputPresentationVisuals();
                    }

                    SetGroupVisible(_opponentVerseGroup, _opponentVerseTransition, _opponentVersePanelImage, true, animate);
                    SetGroupVisible(_rhymeInputGroup, _rhymeInputTransition, _rhymeInputPanelImage, true, animate);
                    SetGroupVisible(_playerVerseGroup, _playerVerseTransition, _playerVersePanelImage, false, animate);

                    if (animate)
                    {
                        BattlePanelShown?.Invoke(BattleUiPanelKind.OpponentVerse);
                        BattlePanelShown?.Invoke(BattleUiPanelKind.RhymeInput);
                    }

                    break;

                case BattlePhase.PlayerVerse:
                    SetGroupVisible(_opponentVerseGroup, _opponentVerseTransition, _opponentVersePanelImage, false, animate);
                    SetGroupVisible(_rhymeInputGroup, _rhymeInputTransition, _rhymeInputPanelImage, false, animate);
                    SetGroupVisible(_playerVerseGroup, _playerVerseTransition, _playerVersePanelImage, true, animate);

                    if (animate)
                    {
                        BattlePanelShown?.Invoke(BattleUiPanelKind.PlayerVerse);
                    }

                    break;

                case BattlePhase.Hidden:
                default:
                    SetGroupVisible(_opponentVerseGroup, _opponentVerseTransition, _opponentVersePanelImage, false, animate);
                    SetGroupVisible(_rhymeInputGroup, _rhymeInputTransition, _rhymeInputPanelImage, false, animate);
                    SetGroupVisible(_playerVerseGroup, _playerVerseTransition, _playerVersePanelImage, false, animate);
                    break;
            }

            _currentPhase = phase;
        }

        private void StartVerseLinePresentation(Text target, Verse verse, BattleUiPanelKind panelKind)
        {
            CancelVerseLinePresentation();

            if (target == null)
            {
                return;
            }

            string rawText = verse == null ? string.Empty : verse.Text;
            IReadOnlyList<VerseHighlight> highlights = verse == null ? null : verse.Highlights;

            if (TryGetVerseLineObjectPresentation(panelKind, out RectTransform lineRoot, out Text lineTemplate, out List<GameObject> lineInstances))
            {
                SetVerseFallbackTextVisible(target, false);
                lineRoot.gameObject.SetActive(true);
                ClearVerseLineInstances(lineInstances);

                if (string.IsNullOrEmpty(rawText))
                {
                    return;
                }

                _verseLinePresentationCts = new CancellationTokenSource();
                var lineObjectCt = _verseLinePresentationCts.Token;

                PresentVerseLineObjectsAsync(
                    lineRoot,
                    lineTemplate,
                    lineInstances,
                    rawText,
                    highlights,
                    panelKind,
                    lineObjectCt
                ).Forget(ex =>
                {
                    if (ex is OperationCanceledException)
                    {
                        return;
                    }

                    Debug.LogException(ex);
                });

                return;
            }

            SetVerseFallbackTextVisible(target, true);
            CaptureVerseLineVisualState(target);

            if (string.IsNullOrEmpty(rawText))
            {
                SetText(target, string.Empty);
                ResetVerseLineVisualState(target);
                return;
            }

            if (!_showVerseLineByLine)
            {
                SetText(target, FormatVerseTextWithHighlights(rawText, highlights));
                BattleVerseLineShown?.Invoke(panelKind);
                ResetVerseLineVisualState(target);
                return;
            }

            _verseLinePresentationCts = new CancellationTokenSource();
            var ct = _verseLinePresentationCts.Token;

            PresentVerseLineByLineAsync(target, rawText, highlights, panelKind, ct).Forget(ex =>
            {
                if (ex is OperationCanceledException)
                {
                    return;
                }

                Debug.LogException(ex);
            });
        }

        private async UniTask PresentVerseLineObjectsAsync(
            RectTransform lineRoot,
            Text lineTemplate,
            List<GameObject> lineInstances,
            string rawText,
            IReadOnlyList<VerseHighlight> highlights,
            BattleUiPanelKind panelKind,
            CancellationToken ct)
        {
            var lineStates = BuildVerseLineTextInstances(lineRoot, lineTemplate, lineInstances, rawText, highlights, hidden: _showVerseLineByLine);
            Canvas.ForceUpdateCanvases();

            try
            {
                BeginVersePresentationGameTimePause();

                if (!_showVerseLineByLine)
                {
                    for (int i = 0; i < lineStates.Count; i++)
                    {
                        BattleVerseLineShown?.Invoke(panelKind);
                    }

                    return;
                }

                for (int i = 0; i < lineStates.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    VerseLineVisualState lineState = lineStates[i];

                    if (lineState.Text == null)
                    {
                        continue;
                    }

                    BattleVerseLineShown?.Invoke(panelKind);

                    float animationDuration = GetVerseLineAnimationDuration();
                    await AnimateVerseLineAppearanceAsync(
                        lineState.Text,
                        animationDuration,
                        ct,
                        lineState.FinalColor,
                        lineState.FinalScale,
                        lineState.CanvasGroup,
                        lineState.FinalCanvasGroupAlpha
                    );

                    if (i < lineStates.Count - 1 && _verseLineIntervalSec > 0f)
                    {
                        float remainingDelay = Mathf.Max(0f, _verseLineIntervalSec - animationDuration);

                        if (remainingDelay > 0f)
                        {
                            await UniTask.Delay(
                                TimeSpan.FromSeconds(remainingDelay),
                                ignoreTimeScale: true,
                                cancellationToken: ct
                            );
                        }
                        else
                        {
                            await UniTask.Yield(PlayerLoopTiming.Update, ct);
                        }
                    }
                    else
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    }
                }
            }
            finally
            {
                ResetActiveVerseLineVisualState();
                EndVersePresentationGameTimePause();
            }
        }

        private async UniTask PresentVerseLineByLineAsync(
            Text target,
            string rawText,
            IReadOnlyList<VerseHighlight> highlights,
            BattleUiPanelKind panelKind,
            CancellationToken ct)
        {
            try
            {
                BeginVersePresentationGameTimePause();

                if (_clearVerseBeforeLinePresentation)
                {
                    SetText(target, string.Empty);
                }

                var revealLengths = BuildVerseLineRevealLengths(rawText);

                for (int i = 0; i < revealLengths.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    int visibleLength = revealLengths[i];
                    SetText(target, FormatVerseTextWithHighlights(rawText, highlights, visibleLength));
                    BattleVerseLineShown?.Invoke(panelKind);

                    float animationDuration = GetVerseLineAnimationDuration();
                    await AnimateVerseLineAppearanceAsync(target, animationDuration, ct);

                    if (i < revealLengths.Count - 1 && _verseLineIntervalSec > 0f)
                    {
                        float remainingDelay = Mathf.Max(0f, _verseLineIntervalSec - animationDuration);

                        if (remainingDelay > 0f)
                        {
                            await UniTask.Delay(
                                TimeSpan.FromSeconds(remainingDelay),
                                ignoreTimeScale: true,
                                cancellationToken: ct
                            );
                        }
                        else
                        {
                            await UniTask.Yield(PlayerLoopTiming.Update, ct);
                        }
                    }
                    else
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    }
                }
            }
            finally
            {
                ResetVerseLineVisualState(target);
                EndVersePresentationGameTimePause();
            }
        }

        private async UniTask AnimateVerseLineAppearanceAsync(Text target, float duration, CancellationToken ct)
        {
            if (target == null || target.rectTransform == null)
            {
                return;
            }

            CanvasGroup canvasGroup = EnsureVerseLineCanvasGroup(target);
            float finalCanvasGroupAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
            await AnimateVerseLineAppearanceAsync(
                target,
                duration,
                ct,
                target.color,
                target.rectTransform.localScale,
                canvasGroup,
                finalCanvasGroupAlpha
            );
        }

        private async UniTask AnimateVerseLineAppearanceAsync(
            Text target,
            float duration,
            CancellationToken ct,
            Color finalColor,
            Vector3 finalScale,
            CanvasGroup canvasGroup,
            float finalCanvasGroupAlpha)
        {
            if (!_animateVerseLineAppearance || target == null)
            {
                if (target != null)
                {
                    target.color = finalColor;

                    if (target.rectTransform != null)
                    {
                        target.rectTransform.localScale = finalScale;
                    }
                }

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = finalCanvasGroupAlpha;
                }

                return;
            }

            RectTransform rectTransform = target.rectTransform;

            if (rectTransform == null)
            {
                return;
            }

            if (canvasGroup == null)
            {
                canvasGroup = EnsureVerseLineCanvasGroup(target);
            }

            CaptureVerseLineVisualState(target, finalColor, finalScale, canvasGroup, finalCanvasGroupAlpha);

            if (duration <= 0f)
            {
                target.color = finalColor;
                rectTransform.localScale = finalScale;

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = finalCanvasGroupAlpha;
                }

                ResetVerseLineVisualState(target);
                return;
            }

            float clampedStartScale = Mathf.Max(0.01f, _verseLineStartScale);
            Vector3 startScale = finalScale * clampedStartScale;
            float startCanvasGroupAlpha = finalCanvasGroupAlpha * Mathf.Clamp01(_verseLineStartAlpha);

            target.color = finalColor;
            rectTransform.localScale = startScale;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = startCanvasGroupAlpha;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);

                target.color = finalColor;
                rectTransform.localScale = Vector3.LerpUnclamped(startScale, finalScale, eased);

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(startCanvasGroupAlpha, finalCanvasGroupAlpha, eased);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            target.color = finalColor;
            rectTransform.localScale = finalScale;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = finalCanvasGroupAlpha;
            }

            ResetVerseLineVisualState(target);
        }

        private float GetVerseLineAnimationDuration()
        {
            if (!_animateVerseLineAppearance)
            {
                return 0f;
            }

            return Mathf.Max(0f, _verseLineFadeInDurationSec);
        }

        private void CaptureVerseLineVisualState(Text target)
        {
            if (target == null)
            {
                _activeVerseLineText = null;
                _activeVerseLineCanvasGroup = null;
                return;
            }

            CaptureVerseLineVisualState(
                target,
                target.color,
                target.rectTransform != null ? target.rectTransform.localScale : Vector3.one,
                EnsureVerseLineCanvasGroup(target),
                EnsureVerseLineCanvasGroup(target) != null ? EnsureVerseLineCanvasGroup(target).alpha : 1f
            );
        }

        private void CaptureVerseLineVisualState(Text target, Color finalColor, Vector3 finalScale, CanvasGroup canvasGroup, float finalCanvasGroupAlpha)
        {
            if (target == null)
            {
                _activeVerseLineText = null;
                _activeVerseLineCanvasGroup = null;
                return;
            }

            _activeVerseLineText = target;
            _activeVerseLineCanvasGroup = canvasGroup;
            _activeVerseLineOriginalColor = finalColor;
            _activeVerseLineOriginalScale = finalScale;
            _activeVerseLineOriginalCanvasGroupAlpha = finalCanvasGroupAlpha;
        }

        private void ResetVerseLineVisualState(Text target)
        {
            if (target == null || _activeVerseLineText != target)
            {
                return;
            }

            target.color = _activeVerseLineOriginalColor;

            if (target.rectTransform != null)
            {
                target.rectTransform.localScale = _activeVerseLineOriginalScale;
            }

            if (_activeVerseLineCanvasGroup != null)
            {
                _activeVerseLineCanvasGroup.alpha = _activeVerseLineOriginalCanvasGroupAlpha;
            }

            _activeVerseLineText = null;
            _activeVerseLineCanvasGroup = null;
        }

        private void ResetActiveVerseLineVisualState()
        {
            ResetVerseLineVisualState(_activeVerseLineText);
        }

        private void BeginVersePresentationGameTimePause()
        {
            if (!_pauseGameTimeDuringVerseLinePresentation || _isVersePresentationGameTimePaused)
            {
                return;
            }

            _versePresentationPreviousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _isVersePresentationGameTimePaused = true;
        }

        private void EndVersePresentationGameTimePause()
        {
            if (!_isVersePresentationGameTimePaused)
            {
                return;
            }

            Time.timeScale = _versePresentationPreviousTimeScale;
            _isVersePresentationGameTimePaused = false;
        }

        private void PrepareVerseLineObjectTemplates()
        {
            PrepareVerseLineObjectTemplate(
                ref _opponentVerseLinesRoot,
                ref _opponentVerseLineTemplate,
                _opponentVerseText,
                "OpponentVerseLinesRoot",
                "OpponentVerseLineTemplate"
            );

            PrepareVerseLineObjectTemplate(
                ref _playerVerseLinesRoot,
                ref _playerVerseLineTemplate,
                _generatedVerseText,
                "PlayerVerseLinesRoot",
                "PlayerVerseLineTemplate"
            );
        }

        private void PrepareVerseLineObjectTemplate(
            ref RectTransform lineRoot,
            ref Text lineTemplate,
            Text fallbackText,
            string rootName,
            string templateName)
        {
            if (_autoBuildVerseLineObjects && lineRoot == null && fallbackText != null && fallbackText.transform.parent != null)
            {
                var rootObject = new GameObject(rootName, typeof(RectTransform));
                rootObject.transform.SetParent(fallbackText.transform.parent, false);

                lineRoot = rootObject.GetComponent<RectTransform>();
                CopyRectTransform(fallbackText.rectTransform, lineRoot);
                lineRoot.SetSiblingIndex(fallbackText.transform.GetSiblingIndex() + 1);
            }

            if (lineRoot != null && lineTemplate == null)
            {
                lineTemplate = lineRoot.GetComponentInChildren<Text>(true);
            }

            if (_autoBuildVerseLineObjects && lineRoot != null && lineTemplate == null && fallbackText != null)
            {
                GameObject templateObject = Instantiate(fallbackText.gameObject, lineRoot);
                templateObject.name = templateName;

                lineTemplate = templateObject.GetComponent<Text>();

                if (lineTemplate != null)
                {
                    ResetRectTransformForLayout(lineTemplate.rectTransform);
                }
            }

            if (lineRoot != null)
            {
                if (_applyVerseLineRootLayout)
                {
                    ApplyVerseLineRootLayout(lineRoot);
                }

                lineRoot.gameObject.SetActive(false);
            }

            if (lineTemplate != null)
            {
                lineTemplate.gameObject.SetActive(false);
            }
        }

        private void CopyRectTransform(RectTransform source, RectTransform target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            target.pivot = source.pivot;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
            target.offsetMin = source.offsetMin;
            target.offsetMax = source.offsetMax;
        }

        private void ResetRectTransformForLayout(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        private void ApplyVerseLineRootLayout(RectTransform lineRoot)
        {
            if (lineRoot == null)
            {
                return;
            }

            if (!lineRoot.TryGetComponent(out VerticalLayoutGroup layoutGroup))
            {
                layoutGroup = lineRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layoutGroup.childAlignment = _verseLineRootChildAlignment;
            layoutGroup.spacing = _verseLineRootSpacing;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = true;
        }

        private bool TryGetVerseLineObjectPresentation(
            BattleUiPanelKind panelKind,
            out RectTransform lineRoot,
            out Text lineTemplate,
            out List<GameObject> lineInstances)
        {
            switch (panelKind)
            {
                case BattleUiPanelKind.OpponentVerse:
                    lineRoot = _opponentVerseLinesRoot;
                    lineTemplate = _opponentVerseLineTemplate;
                    lineInstances = _opponentVerseLineInstances;
                    break;

                case BattleUiPanelKind.PlayerVerse:
                    lineRoot = _playerVerseLinesRoot;
                    lineTemplate = _playerVerseLineTemplate;
                    lineInstances = _playerVerseLineInstances;
                    break;

                default:
                    lineRoot = null;
                    lineTemplate = null;
                    lineInstances = null;
                    break;
            }

            return lineRoot != null && lineTemplate != null && lineInstances != null;
        }

        private void SetVerseFallbackTextVisible(Text target, bool visible)
        {
            if (!_hideFallbackVerseTextWhenUsingLineObjects || target == null)
            {
                return;
            }

            target.gameObject.SetActive(visible);
        }

        private void SetVerseLineObjectPresentationVisible(BattleUiPanelKind panelKind, bool visible)
        {
            if (!TryGetVerseLineObjectPresentation(panelKind, out RectTransform lineRoot, out _, out List<GameObject> lineInstances))
            {
                return;
            }

            if (!visible)
            {
                ClearVerseLineInstances(lineInstances);
            }

            lineRoot.gameObject.SetActive(visible);
        }

        private void HideAllVerseLineObjectPresentations()
        {
            SetVerseLineObjectPresentationVisible(BattleUiPanelKind.OpponentVerse, false);
            SetVerseLineObjectPresentationVisible(BattleUiPanelKind.PlayerVerse, false);
            SetVerseFallbackTextVisible(_opponentVerseText, true);
            SetVerseFallbackTextVisible(_generatedVerseText, true);
        }

        private List<VerseLineVisualState> BuildVerseLineTextInstances(
            RectTransform lineRoot,
            Text lineTemplate,
            List<GameObject> lineInstances,
            string rawText,
            IReadOnlyList<VerseHighlight> highlights,
            bool hidden)
        {
            var lineStates = new List<VerseLineVisualState>();

            if (lineRoot == null || lineTemplate == null || lineInstances == null)
            {
                return lineStates;
            }

            ClearVerseLineInstances(lineInstances);
            List<VerseLineSegment> lineSegments = BuildVerseLineSegments(rawText);

            for (int i = 0; i < lineSegments.Count; i++)
            {
                VerseLineSegment lineSegment = lineSegments[i];

                Text lineText = Instantiate(lineTemplate, lineRoot);
                lineText.name = $"VerseLine_{i + 1}";
                lineText.gameObject.SetActive(true);
                lineText.raycastTarget = false;
                ResetRectTransformForLayout(lineText.rectTransform);

                EnsureVerseLineLayoutElement(lineText);
                SetText(lineText, FormatVerseLineTextWithHighlights(rawText, highlights, lineSegment.StartIndex, lineSegment.Length));

                Color finalColor = lineText.color;
                Vector3 finalScale = lineText.rectTransform != null
                    ? lineText.rectTransform.localScale
                    : Vector3.one;
                CanvasGroup canvasGroup = EnsureVerseLineCanvasGroup(lineText);
                float finalCanvasGroupAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

                if (hidden && canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }

                lineInstances.Add(lineText.gameObject);
                lineStates.Add(new VerseLineVisualState(lineText, canvasGroup, finalColor, finalScale, finalCanvasGroupAlpha));
            }

            if (lineTemplate != null)
            {
                lineTemplate.gameObject.SetActive(false);
            }

            return lineStates;
        }

        private CanvasGroup EnsureVerseLineCanvasGroup(Text lineText)
        {
            if (lineText == null)
            {
                return null;
            }

            if (!lineText.TryGetComponent(out CanvasGroup canvasGroup))
            {
                canvasGroup = lineText.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            return canvasGroup;
        }

        private void EnsureVerseLineLayoutElement(Text lineText)
        {
            if (lineText == null)
            {
                return;
            }

            if (!lineText.TryGetComponent(out LayoutElement layoutElement))
            {
                layoutElement = lineText.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.flexibleWidth = 1f;
            layoutElement.flexibleHeight = 1f;
        }

        private void ClearVerseLineInstances(List<GameObject> lineInstances)
        {
            if (lineInstances == null)
            {
                return;
            }

            for (int i = 0; i < lineInstances.Count; i++)
            {
                GameObject lineObject = lineInstances[i];

                if (lineObject == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(lineObject);
                }
                else
                {
                    DestroyImmediate(lineObject);
                }
            }

            lineInstances.Clear();
        }

        private List<VerseLineSegment> BuildVerseLineSegments(string rawText)
        {
            var lineSegments = new List<VerseLineSegment>();

            if (string.IsNullOrEmpty(rawText))
            {
                return lineSegments;
            }

            int lineStartIndex = 0;

            for (int i = 0; i < rawText.Length; i++)
            {
                if (rawText[i] != '\n')
                {
                    continue;
                }

                int lineLength = i - lineStartIndex;

                if (lineLength > 0 && rawText[lineStartIndex + lineLength - 1] == '\r')
                {
                    lineLength--;
                }

                lineSegments.Add(new VerseLineSegment(lineStartIndex, Mathf.Max(0, lineLength)));
                lineStartIndex = i + 1;
            }

            if (lineStartIndex < rawText.Length)
            {
                int lineLength = rawText.Length - lineStartIndex;

                if (lineLength > 0 && rawText[lineStartIndex + lineLength - 1] == '\r')
                {
                    lineLength--;
                }

                lineSegments.Add(new VerseLineSegment(lineStartIndex, Mathf.Max(0, lineLength)));
            }

            if (lineSegments.Count == 0)
            {
                lineSegments.Add(new VerseLineSegment(0, rawText.Length));
            }

            return lineSegments;
        }

        private List<int> BuildVerseLineRevealLengths(string rawText)
        {
            var revealLengths = new List<int>();

            if (string.IsNullOrEmpty(rawText))
            {
                return revealLengths;
            }

            for (int i = 0; i < rawText.Length; i++)
            {
                if (rawText[i] == '\n')
                {
                    revealLengths.Add(i + 1);
                }
            }

            if (revealLengths.Count == 0 || revealLengths[revealLengths.Count - 1] < rawText.Length)
            {
                revealLengths.Add(rawText.Length);
            }

            return revealLengths;
        }

        private void CancelVerseLinePresentation()
        {
            ResetActiveVerseLineVisualState();
            EndVersePresentationGameTimePause();

            if (_verseLinePresentationCts == null)
            {
                return;
            }

            _verseLinePresentationCts.Cancel();
            _verseLinePresentationCts.Dispose();
            _verseLinePresentationCts = null;
        }

        private async UniTask MoveBattleStartSignalAsync(Vector2 from, Vector2 to, float duration, bool useEaseOut, CancellationToken ct)
        {
            if (_battleStartSignalRectTransform == null)
            {
                return;
            }

            if (duration <= 0f)
            {
                _battleStartSignalRectTransform.anchoredPosition = to;
                return;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = useEaseOut ? EaseOutCubic(t) : EaseInCubic(t);
                _battleStartSignalRectTransform.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            _battleStartSignalRectTransform.anchoredPosition = to;
        }

        private void CancelBattleStartSignal()
        {
            if (_battleStartSignalCts == null)
            {
                return;
            }

            _battleStartSignalCts.Cancel();
            _battleStartSignalCts.Dispose();
            _battleStartSignalCts = null;
            _isBattleStartSignalPlaying = false;
        }

        private Vector2 GetBattleStartSignalEnterPosition()
        {
            return _battleStartSignalCenterPosition + new Vector2(_battleStartSignalEnterOffsetX, 0f);
        }

        private Vector2 GetBattleStartSignalExitPosition()
        {
            return _battleStartSignalCenterPosition + new Vector2(_battleStartSignalExitOffsetX, 0f);
        }

        private void PrepareBattleStartSignal()
        {
            GameObject signalObject = ResolveBattleStartSignalObject();

            if (signalObject == null)
            {
                return;
            }

            if (_battleStartSignalCanvasGroup == null)
            {
                _battleStartSignalCanvasGroup = signalObject.GetComponent<CanvasGroup>();
            }

            if (_battleStartSignalCanvasGroup == null)
            {
                _battleStartSignalCanvasGroup = signalObject.AddComponent<CanvasGroup>();
            }

            if (_battleStartSignalImage == null)
            {
                _battleStartSignalImage = signalObject.GetComponentInChildren<Image>(true);
            }

            if (_battleStartSignalRectTransform == null)
            {
                _battleStartSignalRectTransform = signalObject.transform as RectTransform;
            }

            if (_battleStartSignalRectTransform != null)
            {
                _battleStartSignalCenterPosition = _battleStartSignalRectTransform.anchoredPosition;
            }

            SetBattleStartSignalVisible(false, 0f);
        }

        private GameObject ResolveBattleStartSignalObject()
        {
            if (_battleStartSignalGroup != null)
            {
                return _battleStartSignalGroup;
            }

            if (_battleStartSignalCanvasGroup != null)
            {
                return _battleStartSignalCanvasGroup.gameObject;
            }

            if (_battleStartSignalImage != null)
            {
                return _battleStartSignalImage.gameObject;
            }

            return null;
        }

        private bool HasBattleStartSignal()
        {
            return ResolveBattleStartSignalObject() != null
                && _battleStartSignalCanvasGroup != null
                && _battleStartSignalRectTransform != null;
        }

        private void SetBattleStartSignalVisible(bool visible, float alpha)
        {
            GameObject signalObject = ResolveBattleStartSignalObject();

            if (signalObject != null)
            {
                signalObject.SetActive(visible);
            }

            if (_battleStartSignalCanvasGroup != null)
            {
                _battleStartSignalCanvasGroup.alpha = alpha;
                _battleStartSignalCanvasGroup.interactable = false;
                _battleStartSignalCanvasGroup.blocksRaycasts = false;
            }
        }

        private void HandleRetryButtonClicked()
        {
            RetrySelected?.Invoke();
        }

        /// <summary>
        /// 入力フェーズに新規突入した際の見た目を初期化します．
        /// タイマーが実際に開始するまでは，残り時間・入力済みライムを表示しません．
        /// </summary>
        private void ResetInputPresentationVisuals()
        {
            SetText(_inputTimerText, string.Empty);  // "00:00"ではなく非表示相当の空文字に
            UpdateInputRhymesTextFallback(null);
            RebuildInputRhymeTabs(null);
        }

        private void ApplyBattleBackground()
        {
            if (_backgroundImage == null)
            {
                return;
            }

            if (_battleBackgroundSprite != null)
            {
                _backgroundImage.sprite = _battleBackgroundSprite;
            }

            _backgroundImage.color = _backgroundColor;
            _backgroundImage.type = Image.Type.Simple;
            _backgroundImage.raycastTarget = false;

            _backgroundImage.transform.SetAsFirstSibling();
        }

        private void ApplyVersePanels()
        {
            ApplyPanelImage(
                _opponentVersePanelImage,
                _opponentVersePanelSprite,
                _opponentVersePanelColor,
                _useSlicedVersePanels
            );

            ApplyPanelImage(
                _playerVersePanelImage,
                _playerVersePanelSprite,
                _playerVersePanelColor,
                _useSlicedVersePanels
            );
        }

        private void ApplyRhymeInputPanel()
        {
            ApplyPanelImage(
                _rhymeInputPanelImage,
                _rhymeInputPanelSprite,
                _rhymeInputPanelColor,
                _useSlicedRhymeInputPanel
            );

            if (_inputRhymesScrollRect != null)
            {
                _inputRhymesScrollRect.horizontal = false;
                _inputRhymesScrollRect.vertical = true;
                _inputRhymesScrollRect.movementType = ScrollRect.MovementType.Clamped;
                _inputRhymesScrollRect.scrollSensitivity = Mathf.Max(1f, _inputRhymesScrollSensitivity);
            }
        }

        private void ApplyPanelImage(Image panelImage, Sprite panelSprite, Color panelColor, bool useSliced)
        {
            if (panelImage == null)
            {
                return;
            }

            if (panelSprite != null)
            {
                panelImage.sprite = panelSprite;
            }

            panelImage.color = panelColor;
            panelImage.raycastTarget = false;
            panelImage.type = useSliced ? Image.Type.Sliced : Image.Type.Simple;
            panelImage.fillCenter = true;
        }

        private void ResolvePanelTransitions()
        {
            if (_opponentVerseTransition == null)
            {
                _opponentVerseTransition = ResolveTransition(_opponentVerseGroup, _opponentVersePanelImage);
            }

            if (_playerVerseTransition == null)
            {
                _playerVerseTransition = ResolveTransition(_playerVerseGroup, _playerVersePanelImage);
            }

            if (_rhymeInputTransition == null)
            {
                _rhymeInputTransition = ResolveTransition(_rhymeInputGroup, _rhymeInputPanelImage);
            }
        }

        private UIPanelTransition ResolveTransition(GameObject group, Component fallbackComponent)
        {
            if (group != null && group.TryGetComponent(out UIPanelTransition transition))
            {
                return transition;
            }

            if (fallbackComponent != null && fallbackComponent.TryGetComponent(out UIPanelTransition fallbackTransition))
            {
                return fallbackTransition;
            }

            return null;
        }

        private void CapturePanelTransitionVisualStates()
        {
            _opponentVerseTransition?.CaptureCurrentVisualState();
            _playerVerseTransition?.CaptureCurrentVisualState();
            _rhymeInputTransition?.CaptureCurrentVisualState();
        }

        private void PrepareRhymeTabTemplate()
        {
            if (_inputRhymeTabTemplate != null)
            {
                _inputRhymeTabTemplate.SetActive(false);
            }

            if (_inputRhymesEmptyText != null)
            {
                _inputRhymesEmptyText.gameObject.SetActive(true);
            }
        }

        private void UpdateInputRhymesTextFallback(IReadOnlyList<string> rhymes)
        {
            if (_inputRhymesText == null)
            {
                return;
            }

            if (rhymes == null || rhymes.Count == 0)
            {
                _inputRhymesText.text = "入力済みライム：なし";
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("入力済みライム：");

            for (int i = 0; i < rhymes.Count; i++)
            {
                builder.AppendLine($"{i + 1}. {rhymes[i]}");
            }

            _inputRhymesText.text = builder.ToString();
        }

        private void RebuildInputRhymeTabs(IReadOnlyList<string> rhymes)
        {
            ClearInputRhymeTabs();

            bool hasRhymes = rhymes != null && rhymes.Count > 0;

            if (_inputRhymesEmptyText != null)
            {
                _inputRhymesEmptyText.gameObject.SetActive(!hasRhymes);
            }

            if (!hasRhymes || _inputRhymeTabTemplate == null || _inputRhymesContent == null)
            {
                return;
            }

            if (_inputRhymesScrollRect != null)
            {
                _inputRhymesScrollRect.scrollSensitivity = Mathf.Max(1f, _inputRhymesScrollSensitivity);
            }

            GameObject currentRow = null;
            float rowAvailableWidth = GetRhymeRowAvailableWidth();
            float currentRowUsedWidth = 0f;
            int currentRowItemCount = 0;
            int rowIndex = 0;
            int tabsPerRowFallback = Mathf.Max(1, _rhymeTabsPerRow);

            for (int i = 0; i < rhymes.Count; i++)
            {
                int rhymeIndex = i;
                string rhymeWord = rhymes[i];
                float tabWidth = GetRhymeTabPreferredWidth(rhymeWord, rowAvailableWidth);

                Transform parentTransform = _inputRhymesContent;

                if (_wrapRhymeTabsIntoRows)
                {
                    bool shouldCreateNewRow = currentRow == null;

                    if (!shouldCreateNewRow)
                    {
                        if (_useDynamicRhymeTabWidth)
                        {
                            float projectedWidth = currentRowUsedWidth + (currentRowItemCount > 0 ? Mathf.Max(0f, _rhymeTabHorizontalSpacing) : 0f) + tabWidth;
                            shouldCreateNewRow = currentRowItemCount > 0 && projectedWidth > rowAvailableWidth;
                        }
                        else
                        {
                            shouldCreateNewRow = currentRowItemCount >= tabsPerRowFallback;
                        }
                    }

                    if (shouldCreateNewRow)
                    {
                        currentRow = CreateInputRhymeRow(rowIndex, rowAvailableWidth);
                        currentRowUsedWidth = 0f;
                        currentRowItemCount = 0;
                        rowIndex++;
                    }

                    if (currentRow != null)
                    {
                        parentTransform = currentRow.transform;
                    }
                }

                GameObject tab = Instantiate(_inputRhymeTabTemplate, parentTransform);
                tab.name = $"InputRhymeTab_{i + 1}";
                tab.transform.localScale = Vector3.one;
                PrepareInputRhymeTabLayout(tab, tabWidth);
                tab.SetActive(true);

                Text wordText = FindText(tab.transform, _rhymeTabWordTextName);
                if (wordText != null)
                {
                    ConfigureRhymeTabWordText(wordText, rhymeWord, tabWidth);
                }

                Button removeButton = FindButton(tab.transform, _rhymeTabRemoveButtonName);
                if (removeButton != null)
                {
                    removeButton.onClick.RemoveAllListeners();
                    removeButton.onClick.AddListener(() => InputRhymeRemoveAtRequested?.Invoke(rhymeIndex));
                    removeButton.interactable = true;
                }

                _inputRhymeTabInstances.Add(tab);

                if (_wrapRhymeTabsIntoRows && _useDynamicRhymeTabWidth)
                {
                    currentRowUsedWidth += (currentRowItemCount > 0 ? Mathf.Max(0f, _rhymeTabHorizontalSpacing) : 0f) + tabWidth;
                    currentRowItemCount++;
                }
                else if (_wrapRhymeTabsIntoRows)
                {
                    currentRowItemCount++;
                }
            }

            Canvas.ForceUpdateCanvases();

            if (_inputRhymesScrollRect != null)
            {
                _inputRhymesScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void ClearInputRhymeTabs()
        {
            for (int i = 0; i < _inputRhymeTabInstances.Count; i++)
            {
                GameObject tab = _inputRhymeTabInstances[i];

                if (tab == null)
                {
                    continue;
                }

                bool isDirectContentChild = _inputRhymesContent != null && tab.transform.parent == _inputRhymesContent;

                if (isDirectContentChild)
                {
                    DestroyRhymeUiObject(tab);
                }
            }

            _inputRhymeTabInstances.Clear();

            for (int i = 0; i < _inputRhymeRowInstances.Count; i++)
            {
                DestroyRhymeUiObject(_inputRhymeRowInstances[i]);
            }

            _inputRhymeRowInstances.Clear();
        }

        private GameObject CreateInputRhymeRow(int rowIndex)
        {
            return CreateInputRhymeRow(rowIndex, GetRhymeRowAvailableWidth());
        }

        private GameObject CreateInputRhymeRow(int rowIndex, float rowWidth)
        {
            if (_inputRhymesContent == null)
            {
                return null;
            }

            var rowObject = new GameObject($"InputRhymeRow_{rowIndex + 1}", typeof(RectTransform));
            rowObject.transform.SetParent(_inputRhymesContent, false);
            rowObject.SetActive(true);

            RectTransform rowRectTransform = rowObject.GetComponent<RectTransform>();
            if (rowRectTransform != null)
            {
                rowRectTransform.localScale = Vector3.one;
                rowRectTransform.sizeDelta = new Vector2(rowWidth, GetRhymeTabPreferredHeight());
            }

            HorizontalLayoutGroup horizontalLayoutGroup = rowObject.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            horizontalLayoutGroup.spacing = Mathf.Max(0f, _rhymeTabHorizontalSpacing);
            horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childControlHeight = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.childForceExpandHeight = false;

            LayoutElement layoutElement = rowObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = rowWidth;
            layoutElement.preferredHeight = GetRhymeTabPreferredHeight();
            layoutElement.minHeight = GetRhymeTabPreferredHeight();
            layoutElement.flexibleWidth = 1f;
            layoutElement.flexibleHeight = 0f;

            _inputRhymeRowInstances.Add(rowObject);
            return rowObject;
        }

        private void PrepareInputRhymeTabLayout(GameObject tab)
        {
            PrepareInputRhymeTabLayout(tab, GetRhymeTabPreferredWidth());
        }

        private void PrepareInputRhymeTabLayout(GameObject tab, float tabWidth)
        {
            if (tab == null)
            {
                return;
            }

            tabWidth = Mathf.Max(1f, tabWidth);

            RectTransform rectTransform = tab.transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.sizeDelta = new Vector2(tabWidth, GetRhymeTabPreferredHeight());
            }

            if (!_wrapRhymeTabsIntoRows)
            {
                return;
            }

            LayoutElement layoutElement = tab.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = tab.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = tabWidth;
            layoutElement.preferredHeight = GetRhymeTabPreferredHeight();
            layoutElement.minWidth = tabWidth;
            layoutElement.minHeight = GetRhymeTabPreferredHeight();
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
            layoutElement.ignoreLayout = false;
        }

        private void ConfigureRhymeTabWordText(Text wordText, string rhymeWord, float tabWidth)
        {
            if (wordText == null)
            {
                return;
            }

            wordText.supportRichText = true;
            wordText.text = rhymeWord;

            if (_centerRhymeTabWordText)
            {
                wordText.alignment = TextAnchor.MiddleCenter;
            }

            if (!_shrinkRhymeTextWhenOverflow)
            {
                return;
            }

            int originalFontSize = Mathf.Max(wordText.fontSize, _rhymeTabMinFontSize);
            float availableTextWidth = Mathf.Max(1f, tabWidth - Mathf.Max(0f, _rhymeTabTextHorizontalPadding));
            float preferredTextWidth = CalculateRhymeTextPreferredWidth(wordText, rhymeWord, originalFontSize);

            wordText.resizeTextForBestFit = false;

            if (preferredTextWidth <= availableTextWidth)
            {
                wordText.fontSize = originalFontSize;
                return;
            }

            float scale = availableTextWidth / Mathf.Max(1f, preferredTextWidth);
            int fittedFontSize = Mathf.FloorToInt(originalFontSize * scale);
            wordText.fontSize = Mathf.Clamp(fittedFontSize, Mathf.Max(1, _rhymeTabMinFontSize), originalFontSize);
        }

        private float GetRhymeTabPreferredWidth()
        {
            RectTransform templateRectTransform = _inputRhymeTabTemplate != null
                ? _inputRhymeTabTemplate.transform as RectTransform
                : null;

            if (templateRectTransform != null && templateRectTransform.rect.width > 1f)
            {
                return templateRectTransform.rect.width;
            }

            return 150f;
        }

        private float GetRhymeTabPreferredWidth(string rhymeWord, float rowAvailableWidth)
        {
            if (!_useDynamicRhymeTabWidth)
            {
                return GetRhymeTabPreferredWidth();
            }

            Text templateWordText = _inputRhymeTabTemplate != null
                ? FindText(_inputRhymeTabTemplate.transform, _rhymeTabWordTextName)
                : null;

            int fontSize = templateWordText != null ? templateWordText.fontSize : 24;
            float textWidth = CalculateRhymeTextPreferredWidth(templateWordText, rhymeWord, fontSize);
            float preferredWidth = textWidth + Mathf.Max(0f, _rhymeTabTextHorizontalPadding);

            float safeRowWidth = Mathf.Max(1f, rowAvailableWidth);
            float maxWidth = Mathf.Min(Mathf.Max(1f, _rhymeTabMaxWidth), safeRowWidth);
            float minWidth = Mathf.Min(Mathf.Max(1f, _rhymeTabMinWidth), maxWidth);

            return Mathf.Clamp(preferredWidth, minWidth, maxWidth);
        }

        private float CalculateRhymeTextPreferredWidth(Text sourceText, string text, int fontSize)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            if (sourceText != null)
            {
                try
                {
                    Vector2 generationExtents = new Vector2(10000f, Mathf.Max(1f, GetRhymeTabPreferredHeight()));
                    TextGenerationSettings settings = sourceText.GetGenerationSettings(generationExtents);
                    settings.fontSize = Mathf.Max(1, fontSize);

                    float preferredWidth = sourceText.cachedTextGeneratorForLayout.GetPreferredWidth(text, settings) / sourceText.pixelsPerUnit;

                    if (preferredWidth > 1f && !float.IsNaN(preferredWidth) && !float.IsInfinity(preferredWidth))
                    {
                        return preferredWidth;
                    }
                }
                catch
                {
                    // Fallback to a simple estimate below.
                }
            }

            int safeFontSize = Mathf.Max(1, fontSize);
            float width = 0f;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                width += c <= 0x007f ? safeFontSize * 0.55f : safeFontSize * 0.95f;
            }

            return Mathf.Max(1f, width);
        }

        private float GetRhymeTabPreferredHeight()
        {
            RectTransform templateRectTransform = _inputRhymeTabTemplate != null
                ? _inputRhymeTabTemplate.transform as RectTransform
                : null;

            if (templateRectTransform != null && templateRectTransform.rect.height > 1f)
            {
                return templateRectTransform.rect.height;
            }

            return 48f;
        }

        private float GetRhymeRowPreferredWidth()
        {
            return GetRhymeRowAvailableWidth();
        }

        private float GetRhymeRowAvailableWidth()
        {
            if (_inputRhymesScrollRect != null && _inputRhymesScrollRect.viewport != null)
            {
                float viewportWidth = _inputRhymesScrollRect.viewport.rect.width;

                if (viewportWidth > 1f)
                {
                    return viewportWidth;
                }
            }

            if (_inputRhymesContent != null)
            {
                float contentWidth = _inputRhymesContent.rect.width;

                if (contentWidth > 1f)
                {
                    return contentWidth;
                }

                if (_inputRhymesContent.parent is RectTransform parentRectTransform && parentRectTransform.rect.width > 1f)
                {
                    return parentRectTransform.rect.width;
                }
            }

            return Mathf.Max(1f, _rhymeTabFallbackRowWidth);
        }

        private void DestroyRhymeUiObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private Text FindText(Transform root, string preferredName)
        {
            Transform preferred = FindChildRecursive(root, preferredName);
            if (preferred != null && preferred.TryGetComponent(out Text preferredText))
            {
                return preferredText;
            }

            return root.GetComponentInChildren<Text>(true);
        }

        private Button FindButton(Transform root, string preferredName)
        {
            Transform preferred = FindChildRecursive(root, preferredName);
            if (preferred != null && preferred.TryGetComponent(out Button preferredButton))
            {
                return preferredButton;
            }

            return root.GetComponentInChildren<Button>(true);
        }

        private Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child.name == childName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private void SetGroupVisible(GameObject group, UIPanelTransition transition, Component fallbackComponent, bool visible, bool animate)
        {
            if (transition != null)
            {
                if (animate)
                {
                    if (visible)
                    {
                        transition.Show();
                    }
                    else
                    {
                        transition.Hide();
                    }
                }
                else
                {
                    if (visible)
                    {
                        transition.ShowImmediate();
                    }
                    else
                    {
                        transition.HideImmediate();
                    }
                }

                return;
            }

            if (group != null)
            {
                group.SetActive(visible);
                return;
            }

            if (fallbackComponent != null)
            {
                fallbackComponent.gameObject.SetActive(visible);
            }
        }

        private void SetResultVisible(bool visible)
        {
            if (_resultPanel != null)
            {
                _resultPanel.SetActive(visible);

                if (visible)
                {
                    _resultPanel.transform.SetAsLastSibling();
                }
            }

            if (_retryButton != null)
            {
                _retryButton.gameObject.SetActive(visible);
                _retryButton.interactable = visible;
            }
        }

        private string BuildResultText(BattleResult result)
        {
            object resultObject = result;
            string resultDetail = resultObject?.ToString();

            string defaultTypeName = typeof(BattleResult).ToString();
            string defaultShortTypeName = typeof(BattleResult).Name;

            if (string.IsNullOrWhiteSpace(resultDetail)
                || resultDetail == defaultTypeName
                || resultDetail == defaultShortTypeName)
            {
                return "全ターン終了\nバトルが終了しました";
            }

            return $"全ターン終了\nバトルが終了しました\n\n{resultDetail}";
        }

        private void SetStatus(string status)
        {
            SetText(_statusText, status);
        }

        private void SetText(Text target, string value)
        {
            if (target == null)
            {
                return;
            }

            target.supportRichText = true;
            target.text = value;
        }

        private string FormatTimer(int sec)
        {
            int clampedSec = Mathf.Max(0, sec);
            int minutes = clampedSec / 60;
            int seconds = clampedSec % 60;

            return $"{minutes:00}:{seconds:00}";
        }

        private string FormatVerseLineTextWithHighlights(
            string rawText,
            IReadOnlyList<VerseHighlight> highlights,
            int lineStartIndex,
            int lineLength)
        {
            if (string.IsNullOrEmpty(rawText) || lineLength <= 0)
            {
                return string.Empty;
            }

            lineStartIndex = Mathf.Clamp(lineStartIndex, 0, rawText.Length);
            lineLength = Mathf.Clamp(lineLength, 0, rawText.Length - lineStartIndex);

            string lineRawText = rawText.Substring(lineStartIndex, lineLength);
            string text = EscapeRichText(lineRawText);

            if (highlights != null && highlights.Count > 0)
            {
                var sb = new StringBuilder(text);
                int offset = 0;
                int lineEndIndex = lineStartIndex + lineLength;

                foreach (var highlight in highlights)
                {
                    int highlightStartIndex = highlight.StartIndex;
                    int highlightEndIndex = highlight.StartIndex + highlight.Length;

                    if (highlight.Length <= 0 || highlightEndIndex <= lineStartIndex || highlightStartIndex >= lineEndIndex)
                    {
                        continue;
                    }

                    int overlapStartIndex = Mathf.Max(highlightStartIndex, lineStartIndex);
                    int overlapEndIndex = Mathf.Min(highlightEndIndex, lineEndIndex);
                    int localStartIndex = overlapStartIndex - lineStartIndex + offset;
                    int localLength = overlapEndIndex - overlapStartIndex;

                    if (localLength <= 0 || localStartIndex < 0 || localStartIndex >= sb.Length)
                    {
                        continue;
                    }

                    int localEndIndex = Mathf.Min(localStartIndex + localLength, sb.Length);
                    string highlightedPart = sb.ToString(localStartIndex, localEndIndex - localStartIndex);
                    string replacement = $"<b><color={HighlightColor}>{highlightedPart}</color></b>";
                    sb.Remove(localStartIndex, localEndIndex - localStartIndex);
                    sb.Insert(localStartIndex, replacement);
                    offset += replacement.Length - (localEndIndex - localStartIndex);
                }

                text = sb.ToString();
            }

            return text;
        }

        private string FormatVerseTextWithHighlights(string rawText, IReadOnlyList<VerseHighlight> highlights)
        {
            if (string.IsNullOrEmpty(rawText))
            {
                return string.Empty;
            }

            return FormatVerseTextWithHighlights(rawText, highlights, rawText.Length);
        }

        private string FormatVerseTextWithHighlights(string rawText, IReadOnlyList<VerseHighlight> highlights, int visibleRawLength)
        {
            if (string.IsNullOrEmpty(rawText))
            {
                return string.Empty;
            }

            visibleRawLength = Mathf.Clamp(visibleRawLength, 0, rawText.Length);
            string visibleRawText = rawText.Substring(0, visibleRawLength);
            string text = EscapeRichText(visibleRawText);

            if (highlights != null && highlights.Count > 0)
            {
                var sb = new StringBuilder(text);
                int offset = 0;

                foreach (var highlight in highlights)
                {
                    int rawStartIndex = highlight.StartIndex;
                    int rawLength = highlight.Length;

                    if (rawStartIndex < 0 || rawStartIndex >= visibleRawLength || rawLength <= 0)
                    {
                        continue;
                    }

                    int rawEndIndex = Mathf.Min(rawStartIndex + rawLength, visibleRawLength);
                    int visibleLength = rawEndIndex - rawStartIndex;

                    if (visibleLength <= 0)
                    {
                        continue;
                    }

                    int startIndex = rawStartIndex + offset;

                    if (startIndex < 0 || startIndex >= sb.Length)
                    {
                        continue;
                    }

                    int endIndex = Mathf.Min(startIndex + visibleLength, sb.Length);
                    string highlightedPart = sb.ToString(startIndex, endIndex - startIndex);
                    string replacement = $"<b><color={HighlightColor}>{highlightedPart}</color></b>";
                    sb.Remove(startIndex, endIndex - startIndex);
                    sb.Insert(startIndex, replacement);
                    offset += replacement.Length - (endIndex - startIndex);
                }

                text = sb.ToString();
            }

            return text;
        }

        private string EscapeRichText(string text)
        {
            return text
                .Replace("<", "＜")
                .Replace(">", "＞");
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