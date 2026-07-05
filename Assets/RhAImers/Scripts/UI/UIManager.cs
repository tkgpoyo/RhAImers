using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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

        private enum BattlePhase
        {
            Hidden,
            Input,
            PlayerVerse
        }

        [Header("Background UI")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Sprite _battleBackgroundSprite;
        [SerializeField] private Color _backgroundColor = Color.white;

        [Header("Battle Start Signal")]
        [SerializeField] private bool _showBattleStartSignalOnStart = true;
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

        private readonly List<GameObject> _inputRhymeTabInstances = new();

        private BattlePhase _currentPhase = BattlePhase.Hidden;
        private BattlePhase _pendingPhase = BattlePhase.Input;
        private bool _phaseAnimationReady;
        private bool _isBattleStartSignalPlaying;
        private Coroutine _battleStartSignalCoroutine;
        private Coroutine _inputPresentationReadyCoroutine;
        private Vector2 _battleStartSignalCenterPosition;

        public event Action RetrySelected;
        public event Action<int> InputRhymeRemoveAtRequested;
        public event Action BattleStartSignalShown;
        public event Action<BattleUiPanelKind> BattlePanelShown;
        public event Action BattleResultShown;

        public bool IsInputPresentationReady { get; private set; }

        private void Awake()
        {
            ApplyBattleBackground();
            ApplyVersePanels();
            ApplyRhymeInputPanel();
            PrepareBattleStartSignal();
            PrepareRhymeTabTemplate();
            ResolvePanelTransitions();
            CapturePanelTransitionVisualStates();

            IsInputPresentationReady = false;

            ApplyPhaseVisibility(BattlePhase.Hidden, false);
            SetResultVisible(false);
        }

        private void Start()
        {
            if (_showBattleStartSignalOnStart && HasBattleStartSignal())
            {
                _battleStartSignalCoroutine = StartCoroutine(PlayBattleStartSignalRoutine());
                return;
            }

            _phaseAnimationReady = true;
            ApplyPhaseVisibility(_pendingPhase, true);
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

            if (_battleStartSignalCoroutine != null)
            {
                StopCoroutine(_battleStartSignalCoroutine);
                _battleStartSignalCoroutine = null;
            }

            if (_inputPresentationReadyCoroutine != null)
            {
                StopCoroutine(_inputPresentationReadyCoroutine);
                _inputPresentationReadyCoroutine = null;
            }
        }

        public IEnumerator WaitUntilInputPresentationReady()
        {
            while (!IsInputPresentationReady)
            {
                yield return null;
            }
        }

        public void ShowTitle()
        {
            SetStatus("Title");
            SetResultVisible(false);
        }

        public void ShowModeSelect()
        {
            SetStatus("Mode Select");
            SetResultVisible(false);
        }

        public void ShowOpponentVerse(Verse verse)
        {
            ShowInputPhase();
            SetStatus("Opponent Verse");
            SetResultVisible(false);

            //string text = verse == null ? string.Empty : FormatVerseText(verse.Text);
            string text = verse == null ? string.Empty : FormatVerseTextWithHighlights(verse.Text, verse.Highlights);
            SetText(_opponentVerseText, text);
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

            //string text = verse == null ? string.Empty : FormatVerseText(verse.Text);
            string text = verse == null ? string.Empty : FormatVerseTextWithHighlights(verse.Text, verse.Highlights);
            SetText(_generatedVerseText, text);
        }

        public void ShowResult(BattleResult result)
        {
            HideBattlePhaseGroups();
            SetStatus("Result");
            SetResultVisible(true);
            BattleResultShown?.Invoke();

            string resultText = BuildResultText(result);
            SetText(_resultText, resultText);
        }

        public void ShowOpponentVerseLoading()
        {
            ShowInputPhase();
            SetStatus("Opponent Verse Loading");
            SetResultVisible(false);
            SetText(_opponentVerseText, "相手のバース生成中...");
        }

        public void ShowGenerationLoading()
        {
            ShowPlayerVersePhase();
            SetStatus("Verse Generation Loading");
            SetResultVisible(false);
            SetText(_generatedVerseText, "あなたのバース生成中...");
        }

        public void ShowScoringLoading()
        {
            HideBattlePhaseGroups();
            SetStatus("Scoring Loading");
            SetResultVisible(false);
            SetText(_resultText, "採点中...");
        }

        public void ShowInputPhase()
        {
            RequestPhase(BattlePhase.Input);
        }

        public void ShowPlayerVersePhase()
        {
            RequestPhase(BattlePhase.PlayerVerse);
        }

        public void HideBattlePhaseGroups()
        {
            RequestPhase(BattlePhase.Hidden);
        }

        public void HideBattlePhaseGroupsImmediate()
        {
            ApplyPhaseVisibility(BattlePhase.Hidden, false);
            _pendingPhase = BattlePhase.Hidden;
        }

        private void RequestPhase(BattlePhase phase)
        {
            _pendingPhase = phase;

            if (phase != BattlePhase.Input)
            {
                MarkInputPresentationNotReady();
            }

            if (!_phaseAnimationReady)
            {
                if (phase == BattlePhase.Input)
                {
                    MarkInputPresentationNotReady();
                }

                return;
            }

            ApplyPhaseVisibility(phase, true);
        }

        private void ApplyPhaseVisibility(BattlePhase phase, bool animate)
        {
            if (animate && _currentPhase == phase)
            {
                return;
            }

            switch (phase)
            {
                case BattlePhase.Input:
                    MarkInputPresentationNotReady();
                    SetGroupVisible(_opponentVerseGroup, _opponentVerseTransition, _opponentVersePanelImage, true, animate);
                    SetGroupVisible(_rhymeInputGroup, _rhymeInputTransition, _rhymeInputPanelImage, true, animate);
                    SetGroupVisible(_playerVerseGroup, _playerVerseTransition, _playerVersePanelImage, false, animate);

                    if (animate)
                    {
                        BattlePanelShown?.Invoke(BattleUiPanelKind.OpponentVerse);
                        BattlePanelShown?.Invoke(BattleUiPanelKind.RhymeInput);
                    }

                    StartInputPresentationReadyWatch();
                    break;

                case BattlePhase.PlayerVerse:
                    MarkInputPresentationNotReady();
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
                    MarkInputPresentationNotReady();
                    SetGroupVisible(_opponentVerseGroup, _opponentVerseTransition, _opponentVersePanelImage, false, animate);
                    SetGroupVisible(_rhymeInputGroup, _rhymeInputTransition, _rhymeInputPanelImage, false, animate);
                    SetGroupVisible(_playerVerseGroup, _playerVerseTransition, _playerVersePanelImage, false, animate);
                    break;
            }

            _currentPhase = phase;
        }

        private void StartInputPresentationReadyWatch()
        {
            if (_inputPresentationReadyCoroutine != null)
            {
                StopCoroutine(_inputPresentationReadyCoroutine);
            }

            _inputPresentationReadyCoroutine = StartCoroutine(WaitForInputPresentationReadyRoutine());
        }

        private IEnumerator WaitForInputPresentationReadyRoutine()
        {
            yield return null;

            while (_isBattleStartSignalPlaying
                || IsTransitionRunning(_opponentVerseTransition)
                || IsTransitionRunning(_rhymeInputTransition)
                || IsTransitionRunning(_playerVerseTransition))
            {
                yield return null;
            }

            if (_currentPhase == BattlePhase.Input && _phaseAnimationReady)
            {
                IsInputPresentationReady = true;
            }

            _inputPresentationReadyCoroutine = null;
        }

        private bool IsTransitionRunning(UIPanelTransition transition)
        {
            return transition != null && transition.IsTransitioning;
        }

        private void MarkInputPresentationNotReady()
        {
            IsInputPresentationReady = false;

            if (_inputPresentationReadyCoroutine != null)
            {
                StopCoroutine(_inputPresentationReadyCoroutine);
                _inputPresentationReadyCoroutine = null;
            }
        }

        private IEnumerator PlayBattleStartSignalRoutine()
        {
            _phaseAnimationReady = false;
            _isBattleStartSignalPlaying = true;
            MarkInputPresentationNotReady();
            ApplyPhaseVisibility(BattlePhase.Hidden, false);
            SetBattleStartSignalVisible(true, 1f);

            if (_battleStartSignalRectTransform != null)
            {
                _battleStartSignalRectTransform.anchoredPosition = GetBattleStartSignalEnterPosition();
            }

            BattleStartSignalShown?.Invoke();

            yield return MoveBattleStartSignal(
                GetBattleStartSignalEnterPosition(),
                _battleStartSignalCenterPosition,
                _battleStartSignalSlideInDuration,
                useEaseOut: true
            );

            if (_battleStartSignalHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(_battleStartSignalHoldDuration);
            }

            yield return MoveBattleStartSignal(
                _battleStartSignalCenterPosition,
                GetBattleStartSignalExitPosition(),
                _battleStartSignalSlideOutDuration,
                useEaseOut: false
            );

            SetBattleStartSignalVisible(false, 0f);

            if (_battleStartSignalRectTransform != null)
            {
                _battleStartSignalRectTransform.anchoredPosition = _battleStartSignalCenterPosition;
            }

            if (_battleStartSignalPostDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(_battleStartSignalPostDelay);
            }

            _isBattleStartSignalPlaying = false;
            _phaseAnimationReady = true;
            ApplyPhaseVisibility(_pendingPhase, true);

            _battleStartSignalCoroutine = null;
        }

        private IEnumerator MoveBattleStartSignal(Vector2 from, Vector2 to, float duration, bool useEaseOut)
        {
            if (_battleStartSignalRectTransform == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                _battleStartSignalRectTransform.anchoredPosition = to;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = useEaseOut ? EaseOutCubic(t) : EaseInCubic(t);
                _battleStartSignalRectTransform.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
                yield return null;
            }

            _battleStartSignalRectTransform.anchoredPosition = to;
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

            for (int i = 0; i < rhymes.Count; i++)
            {
                int rhymeIndex = i;
                string rhymeWord = rhymes[i];

                GameObject tab = Instantiate(_inputRhymeTabTemplate, _inputRhymesContent);
                tab.name = $"InputRhymeTab_{i + 1}";
                tab.transform.localScale = Vector3.one;
                tab.SetActive(true);

                Text wordText = FindText(tab.transform, _rhymeTabWordTextName);
                if (wordText != null)
                {
                    wordText.supportRichText = true;
                    wordText.text = rhymeWord;
                }

                Button removeButton = FindButton(tab.transform, _rhymeTabRemoveButtonName);
                if (removeButton != null)
                {
                    removeButton.onClick.RemoveAllListeners();
                    removeButton.onClick.AddListener(() => InputRhymeRemoveAtRequested?.Invoke(rhymeIndex));
                    removeButton.interactable = true;
                }

                _inputRhymeTabInstances.Add(tab);
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

                if (Application.isPlaying)
                {
                    Destroy(tab);
                }
                else
                {
                    DestroyImmediate(tab);
                }
            }

            _inputRhymeTabInstances.Clear();
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

        /// <summary>
        /// テキストのハイライト処理を行います．
        /// </summary>
        /// <remarks>
        /// プロンプトの与え方を変更し，[[]]で囲む処理を削除したため，このメソッドは非推奨となりました．
        /// </remarks>
        /// <param name="rawText"></param>
        /// <returns></returns>
        [Obsolete]
        private string FormatVerseText(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
            {
                return string.Empty;
            }

            string text = EscapeRichText(rawText);

            text = Regex.Replace(
                text,
                @"\[\[(.+?)\]\]",
                $"<b><color={HighlightColor}>$1</color></b>"
            );

            text = Regex.Replace(
                text,
                @"【(.+?)】",
                $"<b><color={HighlightColor}>$1</color></b>"
            );

            return text;
        }

        private string FormatVerseTextWithHighlights(string rawText, IReadOnlyList<VerseHighlight> highlights)
        {
            if (string.IsNullOrEmpty(rawText)) {
                return string.Empty;
            }
            string text = EscapeRichText(rawText);
            if (highlights != null && highlights.Count > 0) {
                var sb = new StringBuilder(text);
                int offset = 0;
                foreach (var highlight in highlights) {
                    int startIndex = highlight.StartIndex + offset;
                    int length = highlight.Length;
                    if (startIndex < 0 || startIndex >= sb.Length || length <= 0) {
                        continue;
                    }
                    int endIndex = Mathf.Min(startIndex + length, sb.Length);
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
