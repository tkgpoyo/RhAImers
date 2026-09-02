using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using RhAImers.Battle;
using RhAImers.Core;
using RhAImers.Scoring;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Result Ranking Comment Slide Controller")]
    public sealed class ResultRankingCommentSlideController : MonoBehaviour
    {
        [Header("Pages")]
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private RectTransform _scorePage;
        [SerializeField] private CanvasGroup _scorePageCanvasGroup;
        [SerializeField] private RectTransform _rankingCommentPage;
        [SerializeField] private CanvasGroup _rankingCommentCanvasGroup;
        [SerializeField] private bool _placeRankingPageOffscreenOnAwake = true;
        [SerializeField] private bool _startOnScorePage = true;
        [SerializeField] private bool _disableHiddenPageInteraction = true;

        [Header("Slide")]
        [SerializeField] private float _slideDuration = 0.45f;
        [SerializeField] private bool _useUnscaledTime = true;
        [SerializeField] private AnimationCurve _slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float _scorePageOpenAlpha = 1f;
        [SerializeField] private float _scorePageClosedAlpha = 0.25f;
        [SerializeField] private float _rankingPageOpenAlpha = 1f;
        [SerializeField] private float _rankingPageClosedAlpha;

        [Header("Legacy Toggle Button")]
        [SerializeField] private Button _toggleButton;
        [SerializeField] private RectTransform _toggleButtonRoot;
        [SerializeField] private TextMeshProUGUI _toggleButtonLabel;
        [SerializeField] private bool _useLegacyToggleButton;

        [Header("Separate Navigation Buttons")]
        [SerializeField] private Button _openRankingButton;
        [SerializeField] private CanvasGroup _openRankingButtonCanvasGroup;
        [SerializeField] private Button _closeScoreButton;
        [SerializeField] private CanvasGroup _closeScoreButtonCanvasGroup;
        [SerializeField] private string _openRankingLabel = "RANKING";
        [SerializeField] private string _closeRankingLabel = "SCORE";
        [SerializeField] private bool _hideInactiveNavigationButton = true;
        [SerializeField] private bool _hideNavigationButtonsDuringSlide = true;
        [SerializeField] private bool _moveToggleButtonDuringSlide = true;
        [SerializeField] private Vector2 _toggleButtonOpenOffset = new Vector2(-80f, 0f);

        [Header("Ranking Texts")]
        [SerializeField] private TextMeshProUGUI _rankingListText;
        [SerializeField] private TextMeshProUGUI _currentScoreText;
        [SerializeField] private TextMeshProUGUI _bestScoreText;
        [SerializeField] private ResultRankingScrollList _rankingScrollList;
        [SerializeField] private bool _autoFindRankingScrollList = true;
        [SerializeField] private int _rankingDisplayCount = 10;
        [SerializeField] private bool _markCurrentScore = true;
        [SerializeField] private string _currentScoreMarker = "  YOU";
        [SerializeField] private string _emptyRankingText = "NO DATA";

        [Header("Comment Text")]
        [SerializeField] private TextMeshProUGUI _commentText;
        [SerializeField] private ScrollRect _commentScrollRect;
        [SerializeField] private RectTransform _commentContent;
        [SerializeField] private bool _autoFindCommentScrollRect = true;
        [SerializeField] private bool _applyCommentScrollRectLayout = true;
        [SerializeField] private bool _forceCommentContentPreferredHeight = true;
        [SerializeField] private bool _resetCommentScrollToTopOnRefresh = false;
        [SerializeField] private bool _ensureCommentViewportMask = true;
        [SerializeField] private bool _ensureCommentViewportRaycastTarget = true;
        [SerializeField] private bool _autoFindCommentVerticalScrollbar = true;
        [SerializeField] private bool _keepCommentVerticalScrollbarVisible = true;
        [SerializeField] private float _commentScrollSensitivity = 60f;
        [SerializeField] private Vector2 _commentTextPadding = new Vector2(0f, 0f);
        [SerializeField] private float _commentBottomPadding = 24f;
        [SerializeField] private bool _tryReadJudgeFeedbackFromGameManager = true;
        [SerializeField] private bool _generateCommentFromScores = true;
        [TextArea(2, 5)]
        [SerializeField] private string _fallbackComment = "\u4eca\u56de\u306e\u30e9\u30c3\u30d7\u30c7\u30fc\u30bf\u306f\u307e\u3060\u3042\u308a\u307e\u305b\u3093\u3002";

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _openClip;
        [SerializeField] private AudioClip _closeClip;
        [SerializeField] private float _openVolume = 0.8f;
        [SerializeField] private float _closeVolume = 0.8f;

        private const BindingFlags FeedbackBindingFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private Coroutine _slideCoroutine;
        private Vector2 _scorePageCenterPosition;
        private Vector2 _rankingPageCenterPosition;
        private Vector2 _toggleButtonClosedPosition;
        private bool _layoutCaptured;
        private bool _rankingOpen;
        private bool _isSliding;
        private bool _slideTargetOpen;
        private float _slideProgress;
        private Coroutine _commentScrollResetCoroutine;

        private void Reset()
        {
            _viewport = transform as RectTransform;
            _audioSource = GetComponent<AudioSource>();
        }

        private void Awake()
        {
            ResolveReferences();
            CaptureLayout();
            RefreshContent();
            SetSlideProgress(_startOnScorePage ? 0f : 1f, true);
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_openRankingButton != null)
            {
                _openRankingButton.onClick.AddListener(OpenRanking);
            }

            if (_closeScoreButton != null)
            {
                _closeScoreButton.onClick.AddListener(CloseRanking);
            }

            if (_useLegacyToggleButton && _toggleButton != null)
            {
                _toggleButton.onClick.AddListener(Toggle);
            }
        }

        private void OnDisable()
        {
            if (_openRankingButton != null)
            {
                _openRankingButton.onClick.RemoveListener(OpenRanking);
            }

            if (_closeScoreButton != null)
            {
                _closeScoreButton.onClick.RemoveListener(CloseRanking);
            }

            if (_useLegacyToggleButton && _toggleButton != null)
            {
                _toggleButton.onClick.RemoveListener(Toggle);
            }

            if (_slideCoroutine != null)
            {
                StopCoroutine(_slideCoroutine);
                _slideCoroutine = null;
            }

            if (_commentScrollResetCoroutine != null)
            {
                StopCoroutine(_commentScrollResetCoroutine);
                _commentScrollResetCoroutine = null;
            }
        }

        [ContextMenu("Refresh Content")]
        public void RefreshContent()
        {
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            BattleResult result = gameManager != null ? gameManager.LastBattleResult : null;
            List<int> ranking = global::RankingManager.GetRanking();
            int currentScore = result != null ? result.TotalScore : -1;

            if (_currentScoreText != null)
            {
                _currentScoreText.text = currentScore >= 0 ? currentScore.ToString() : "-";
            }

            if (_bestScoreText != null)
            {
                _bestScoreText.text = ranking != null && ranking.Count > 0 ? ranking[0].ToString() : "-";
            }

            if (_rankingListText != null)
            {
                _rankingListText.text = BuildRankingText(ranking, currentScore);
            }

            if (_rankingScrollList != null)
            {
                _rankingScrollList.Refresh(result, ranking);
            }

            if (_commentText != null)
            {
                _commentText.text = BuildCommentText(gameManager, result, ranking);
                RefreshCommentScrollLayout();
            }
        }

        [ContextMenu("Open Ranking")]
        public void OpenRanking()
        {
            SetRankingOpen(true);
        }

        [ContextMenu("Close Ranking")]
        public void CloseRanking()
        {
            SetRankingOpen(false);
        }

        public void Toggle()
        {
            SetRankingOpen(!_rankingOpen);
        }

        public void SetRankingOpen(bool open)
        {
            ResolveReferences();
            CaptureLayout();

            if (_slideCoroutine == null && IsAtTargetState(open))
            {
                SetNavigationButtonsForState(open, false);
                return;
            }

            if (open)
            {
                RefreshContent();
            }

            if (_slideCoroutine != null)
            {
                StopCoroutine(_slideCoroutine);
            }

            PlayOneShot(open ? _openClip : _closeClip, open ? _openVolume : _closeVolume);
            _slideCoroutine = StartCoroutine(SlideRoutine(open));
        }

        private IEnumerator SlideRoutine(bool open)
        {
            float start = _slideProgress;
            float end = open ? 1f : 0f;
            float duration = Mathf.Clamp(_slideDuration, 0f, 5f);

            _isSliding = true;
            _slideTargetOpen = open;
            SetPageInput(false, false);
            SetNavigationButtonsForState(open, true);

            if (duration <= 0f)
            {
                SetSlideProgress(end, true);
                _isSliding = false;
                SetNavigationButtonsForState(open, false);
                _slideCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = _slideCurve != null ? _slideCurve.Evaluate(t) : EaseOutCubic(t);
                SetSlideProgress(Mathf.Lerp(start, end, eased), false);
                yield return null;
            }

            SetSlideProgress(end, true);
            _isSliding = false;
            SetNavigationButtonsForState(open, false);
            _slideCoroutine = null;
        }

        private void SetSlideProgress(float progress, bool finalState)
        {
            ResolveReferences();
            CaptureLayout();

            _slideProgress = Mathf.Clamp01(progress);
            _rankingOpen = _slideProgress >= 0.5f;

            float width = ResolveSlideWidth();

            if (_scorePage != null)
            {
                _scorePage.anchoredPosition = _scorePageCenterPosition + Vector2.left * width * _slideProgress;
            }

            if (_rankingCommentPage != null)
            {
                _rankingCommentPage.anchoredPosition =
                    _rankingPageCenterPosition + Vector2.right * width * (1f - _slideProgress);
            }

            if (_scorePageCanvasGroup != null)
            {
                _scorePageCanvasGroup.alpha = Mathf.Lerp(_scorePageOpenAlpha, _scorePageClosedAlpha, _slideProgress);
            }

            if (_rankingCommentCanvasGroup != null)
            {
                _rankingCommentCanvasGroup.alpha = Mathf.Lerp(_rankingPageClosedAlpha, _rankingPageOpenAlpha, _slideProgress);
            }

            if (_moveToggleButtonDuringSlide && _toggleButtonRoot != null)
            {
                _toggleButtonRoot.anchoredPosition = Vector2.Lerp(
                    _toggleButtonClosedPosition,
                    _toggleButtonClosedPosition + _toggleButtonOpenOffset,
                    _slideProgress
                );
            }

            UpdateToggleLabel();
            SetNavigationButtonsForState(_isSliding ? _slideTargetOpen : _rankingOpen, !finalState && _isSliding);

            if (finalState)
            {
                bool open = _slideProgress >= 0.999f;
                _rankingOpen = open;
                SetPageInput(!open, open);
                SetNavigationButtonsForState(open, false);

                if (open)
                {
                    RefreshCommentScrollLayout();
                }
            }
        }

        private void ResolveReferences()
        {
            if (_viewport == null)
            {
                _viewport = transform as RectTransform;
            }

            if (_scorePageCanvasGroup == null && _scorePage != null)
            {
                _scorePageCanvasGroup = EnsureCanvasGroup(_scorePage.gameObject);
            }

            if (_rankingCommentCanvasGroup == null && _rankingCommentPage != null)
            {
                _rankingCommentCanvasGroup = EnsureCanvasGroup(_rankingCommentPage.gameObject);
            }

            if (_toggleButtonRoot == null && _toggleButton != null)
            {
                _toggleButtonRoot = _toggleButton.transform as RectTransform;
            }

            if (_toggleButton == null && _toggleButtonRoot != null)
            {
                _toggleButton = _toggleButtonRoot.GetComponent<Button>();
            }

            if (_openRankingButton == null && _toggleButton != null)
            {
                _openRankingButton = _toggleButton;
            }

            ResolveButtonCanvasGroup(_openRankingButton, ref _openRankingButtonCanvasGroup);
            ResolveButtonCanvasGroup(_closeScoreButton, ref _closeScoreButtonCanvasGroup);

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            if (_rankingScrollList == null && _rankingCommentPage != null)
            {
                _rankingScrollList = _rankingCommentPage.GetComponentInChildren<ResultRankingScrollList>(true);
            }

            if (_rankingScrollList == null && _autoFindRankingScrollList)
            {
                _rankingScrollList = FindFirstObjectByType<ResultRankingScrollList>();
            }

            ResolveCommentScrollReferences();
        }

        private void CaptureLayout()
        {
            if (_layoutCaptured)
            {
                return;
            }

            if (_scorePage != null)
            {
                _scorePageCenterPosition = _scorePage.anchoredPosition;
            }

            if (_rankingCommentPage != null)
            {
                _rankingPageCenterPosition = _placeRankingPageOffscreenOnAwake && _scorePage != null
                    ? _scorePageCenterPosition
                    : _rankingCommentPage.anchoredPosition;
            }

            if (_toggleButtonRoot != null)
            {
                _toggleButtonClosedPosition = _toggleButtonRoot.anchoredPosition;
            }

            _layoutCaptured = true;
        }

        private bool IsAtTargetState(bool open)
        {
            if (open)
            {
                return _rankingOpen && _slideProgress >= 0.999f;
            }

            return !_rankingOpen && _slideProgress <= 0.001f;
        }

        private void SetNavigationButtonsForState(bool rankingOpen, bool sliding)
        {
            bool hideDuringSlide = sliding && _hideNavigationButtonsDuringSlide;
            bool showOpenRankingButton = !rankingOpen && !hideDuringSlide;
            bool showCloseScoreButton = rankingOpen && !hideDuringSlide;
            bool inputEnabled = !sliding;

            SetNavigationButtonVisible(_openRankingButton, _openRankingButtonCanvasGroup, showOpenRankingButton, inputEnabled);
            SetNavigationButtonVisible(_closeScoreButton, _closeScoreButtonCanvasGroup, showCloseScoreButton, inputEnabled);

            if (_useLegacyToggleButton && _toggleButton != null)
            {
                _toggleButton.interactable = inputEnabled;
            }
        }

        private void SetNavigationButtonVisible(Button button, CanvasGroup canvasGroup, bool visible, bool inputEnabled)
        {
            bool active = visible || !_hideInactiveNavigationButton;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = active ? 1f : 0f;
                canvasGroup.interactable = visible && inputEnabled;
                canvasGroup.blocksRaycasts = visible && inputEnabled;
            }

            if (button != null)
            {
                button.interactable = visible && inputEnabled;
            }
        }

        private static void ResolveButtonCanvasGroup(Button button, ref CanvasGroup canvasGroup)
        {
            if (canvasGroup == null && button != null)
            {
                canvasGroup = button.GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null && button != null)
            {
                canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private float ResolveSlideWidth()
        {
            RectTransform target = _viewport != null
                ? _viewport
                : (_scorePage != null ? _scorePage.parent as RectTransform : transform as RectTransform);

            if (target == null)
            {
                return Screen.width;
            }

            float width = target.rect.width;
            return width > 1f ? width : Screen.width;
        }

        private void SetPageInput(bool scoreEnabled, bool rankingEnabled)
        {
            if (!_disableHiddenPageInteraction)
            {
                return;
            }

            SetCanvasGroupInput(_scorePageCanvasGroup, scoreEnabled);
            SetCanvasGroupInput(_rankingCommentCanvasGroup, rankingEnabled);
        }

        private static void SetCanvasGroupInput(CanvasGroup canvasGroup, bool enabled)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
        }

        private void UpdateToggleLabel()
        {
            if (_toggleButtonLabel == null)
            {
                return;
            }

            _toggleButtonLabel.text = _rankingOpen ? _closeRankingLabel : _openRankingLabel;
        }

        private string BuildRankingText(IReadOnlyList<int> ranking, int currentScore)
        {
            if (ranking == null || ranking.Count == 0)
            {
                return _emptyRankingText;
            }

            int displayCount = Mathf.Clamp(_rankingDisplayCount, 1, 100);
            int count = Mathf.Min(displayCount, ranking.Count);
            bool currentMarked = false;
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    builder.AppendLine();
                }

                int score = ranking[i];
                builder.Append(i + 1);
                builder.Append(". ");
                builder.Append(score);

                if (_markCurrentScore && !currentMarked && currentScore >= 0 && score == currentScore)
                {
                    builder.Append(_currentScoreMarker);
                    currentMarked = true;
                }
            }

            return builder.ToString();
        }

        private string BuildCommentText(GameManager gameManager, BattleResult result, IReadOnlyList<int> ranking)
        {
            if (_tryReadJudgeFeedbackFromGameManager)
            {
                string judgeFeedback = TryReadJudgeFeedback(gameManager);
                if (!string.IsNullOrWhiteSpace(judgeFeedback))
                {
                    return judgeFeedback.Trim();
                }
            }

            if (!_generateCommentFromScores)
            {
                return _fallbackComment;
            }

            return BuildAutomaticComment(result, ranking);
        }

        private static string TryReadJudgeFeedback(GameManager gameManager)
        {
            string feedbackFromGameManager = TryReadJudgeFeedbackFromGameManager(gameManager);
            if (!string.IsNullOrWhiteSpace(feedbackFromGameManager))
            {
                return feedbackFromGameManager;
            }

            if (JudgeFeedbackLogCache.TryGet(out string cachedFeedback))
            {
                return cachedFeedback;
            }

            return string.Empty;
        }

        private static string TryReadJudgeFeedbackFromGameManager(GameManager gameManager)
        {
            if (gameManager == null)
            {
                return string.Empty;
            }

            Type type = gameManager.GetType();
            PropertyInfo property = type.GetProperty("LastJudgeFeedback", FeedbackBindingFlags);
            if (property != null && property.PropertyType == typeof(string))
            {
                return property.GetValue(gameManager) as string;
            }

            FieldInfo field = type.GetField("LastJudgeFeedback", FeedbackBindingFlags)
                ?? type.GetField("_lastJudgeFeedback", FeedbackBindingFlags)
                ?? type.GetField("lastJudgeFeedback", FeedbackBindingFlags);

            if (field != null && field.FieldType == typeof(string))
            {
                return field.GetValue(gameManager) as string;
            }

            return string.Empty;
        }

        private string BuildAutomaticComment(BattleResult result, IReadOnlyList<int> ranking)
        {
            if (result == null || result.TurnScores == null || result.TurnScores.Count == 0)
            {
                return _fallbackComment;
            }

            IReadOnlyList<TurnScore> scores = result.TurnScores;
            int bestIndex = 0;
            int bestTotal = scores[0].Total;
            float hardnessSum = 0f;
            float lengthSum = 0f;

            for (int i = 0; i < scores.Count; i++)
            {
                TurnScore score = scores[i];
                hardnessSum += score.AverageHardness;
                lengthSum += score.AverageLength;

                if (score.Total > bestTotal)
                {
                    bestTotal = score.Total;
                    bestIndex = i;
                }
            }

            float averageHardness = hardnessSum / scores.Count;
            float averageLength = lengthSum / scores.Count;

            StringBuilder builder = new StringBuilder();
            builder.Append("TOTAL ");
            builder.Append(result.TotalScore);
            builder.AppendLine();

            builder.Append("\u4e00\u756a\u4f38\u3073\u305f\u306e\u306f TURN ");
            builder.Append(bestIndex + 1);
            builder.Append("\u3002");

            if (averageHardness >= 0.75f)
            {
                builder.Append("\u97fb\u306e\u786c\u3055\u304c\u5f37\u304f\u3001\u97f3\u306e\u82af\u304c\u306f\u3063\u304d\u308a\u898b\u3048\u3066\u3044\u307e\u3059\u3002");
            }
            else if (averageHardness >= 0.5f)
            {
                builder.Append("\u97f3\u306e\u786c\u3055\u306f\u5b89\u5b9a\u3057\u3066\u3044\u307e\u3059\u3002\u6b21\u306f\u8a9e\u5c3e\u306e\u97fb\u3092\u3055\u3089\u306b\u63c3\u3048\u308b\u3068\u4f38\u3073\u307e\u3059\u3002");
            }
            else
            {
                builder.Append("\u97f3\u306e\u8fd1\u3055\u3092\u5897\u3084\u3057\u3066\u3001\u8fd1\u3044\u97fb\u306e\u7d44\u307f\u5408\u308f\u305b\u3092\u72d9\u3046\u3068\u70b9\u304c\u4f38\u3073\u307e\u3059\u3002");
            }

            builder.AppendLine();

            if (averageLength >= 4f)
            {
                builder.Append("\u9577\u3081\u306e\u97f3\u3092\u4f7f\u3048\u3066\u3044\u308b\u306e\u3067\u3001\u8a00\u8449\u306e\u91cd\u307f\u304c\u51fa\u3066\u3044\u307e\u3059\u3002");
            }
            else if (averageLength >= 2.5f)
            {
                builder.Append("\u9577\u3055\u306f\u6a19\u6e96\u7684\u3067\u3059\u3002\u3082\u3046\u4e00\u6bb5\u9577\u3044\u97f3\u3092\u6df7\u305c\u308b\u3068\u5370\u8c61\u304c\u5f37\u304f\u306a\u308a\u307e\u3059\u3002");
            }
            else
            {
                builder.Append("\u77ed\u3044\u97f3\u304c\u4e2d\u5fc3\u3067\u3059\u3002\u4e8c\u8a9e\u4ee5\u4e0a\u306e\u7d44\u307f\u5408\u308f\u305b\u3082\u8a66\u3059\u3068\u8feb\u529b\u304c\u51fa\u307e\u3059\u3002");
            }

            if (ranking != null && ranking.Count > 0 && result.TotalScore >= ranking[0])
            {
                builder.AppendLine();
                builder.Append("\u30e9\u30f3\u30ad\u30f3\u30b0\u4e0a\u4f4d\u306b\u5165\u308b\u52e2\u3044\u3067\u3059\u3002");
            }

            return builder.ToString();
        }

        private void ResolveCommentScrollReferences()
        {
            if (_commentText == null)
            {
                return;
            }

            if (_commentScrollRect == null && _autoFindCommentScrollRect)
            {
                _commentScrollRect = GetComponentInParentIncludingInactive<ScrollRect>(_commentText.transform);
            }

            RectTransform commentTextRect = _commentText.rectTransform;

            if (_commentContent == null)
            {
                if (_commentScrollRect != null && _commentScrollRect.content != null)
                {
                    _commentContent = _commentScrollRect.content;
                }
                else
                {
                    _commentContent = commentTextRect;
                }
            }

            if (_commentScrollRect != null)
            {
                _commentScrollRect.content = _commentContent;
                _commentScrollRect.horizontal = false;
                _commentScrollRect.vertical = true;
                _commentScrollRect.movementType = ScrollRect.MovementType.Clamped;
                _commentScrollRect.scrollSensitivity = Mathf.Max(0f, _commentScrollSensitivity);

                if (_commentScrollRect.verticalScrollbar == null && _autoFindCommentVerticalScrollbar)
                {
                    _commentScrollRect.verticalScrollbar = FindLikelyVerticalScrollbar(_commentScrollRect.transform);
                }

                if (_keepCommentVerticalScrollbarVisible && _commentScrollRect.verticalScrollbar != null)
                {
                    _commentScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                }

                if (_commentScrollRect.viewport != null && _ensureCommentViewportMask)
                {
                    EnsureRectMask(_commentScrollRect.viewport.gameObject);
                }

                if (_commentScrollRect.viewport != null && _ensureCommentViewportRaycastTarget)
                {
                    EnsureRaycastTargetGraphic(_commentScrollRect.viewport.gameObject);
                }
            }

            ApplyCommentScrollRectLayout();
        }

        private void RefreshCommentScrollLayout()
        {
            if (_commentText == null)
            {
                return;
            }

            ResolveCommentScrollReferences();

            _commentText.enableWordWrapping = true;
            _commentText.overflowMode = TextOverflowModes.Overflow;
            ApplyCommentScrollRectLayout();

            if (_forceCommentContentPreferredHeight)
            {
                ApplyCommentPreferredHeight();
            }

            Canvas.ForceUpdateCanvases();

            if (_commentContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_commentContent);
            }

            if (!_resetCommentScrollToTopOnRefresh || _commentScrollRect == null)
            {
                return;
            }

            ResetCommentScrollToTopImmediate();

            if (isActiveAndEnabled)
            {
                if (_commentScrollResetCoroutine != null)
                {
                    StopCoroutine(_commentScrollResetCoroutine);
                }

                _commentScrollResetCoroutine = StartCoroutine(ResetCommentScrollToTopNextFrame());
            }
        }

        private void ApplyCommentScrollRectLayout()
        {
            if (!_applyCommentScrollRectLayout || _commentText == null)
            {
                return;
            }

            RectTransform textRect = _commentText.rectTransform;

            if (_commentScrollRect != null && _commentScrollRect.viewport != null && _commentContent != null)
            {
                _commentContent.anchorMin = new Vector2(0f, 1f);
                _commentContent.anchorMax = new Vector2(1f, 1f);
                _commentContent.pivot = new Vector2(0.5f, 1f);
                _commentContent.anchoredPosition = Vector2.zero;
                _commentContent.offsetMin = new Vector2(0f, _commentContent.offsetMin.y);
                _commentContent.offsetMax = new Vector2(0f, _commentContent.offsetMax.y);
            }

            if (_commentContent != null && textRect != _commentContent)
            {
                float horizontalPadding = Mathf.Max(0f, _commentTextPadding.x);
                float topPadding = Mathf.Max(0f, _commentTextPadding.y);
                float bottomPadding = Mathf.Max(0f, _commentBottomPadding);

                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.pivot = new Vector2(0.5f, 1f);
                textRect.offsetMin = new Vector2(horizontalPadding, bottomPadding);
                textRect.offsetMax = new Vector2(-horizontalPadding, -topPadding);
            }
            else
            {
                textRect.anchorMin = new Vector2(0f, 1f);
                textRect.anchorMax = new Vector2(1f, 1f);
                textRect.pivot = new Vector2(0.5f, 1f);
                textRect.offsetMin = new Vector2(0f, textRect.offsetMin.y);
                textRect.offsetMax = new Vector2(0f, textRect.offsetMax.y);
            }

            _commentText.alignment = NormalizeCommentTextAlignment(_commentText.alignment);
            _commentText.raycastTarget = true;
        }

        private void ApplyCommentPreferredHeight()
        {
            if (_commentText == null)
            {
                return;
            }

            RectTransform textRect = _commentText.rectTransform;
            RectTransform targetContent = _commentContent != null ? _commentContent : textRect;
            float availableWidth = GetCommentAvailableWidth(textRect, targetContent);

            if (availableWidth <= 1f)
            {
                return;
            }

            float topPadding = Mathf.Max(0f, _commentTextPadding.y);
            float bottomPadding = Mathf.Max(0f, _commentBottomPadding);
            float preferredHeight = _commentText.GetPreferredValues(_commentText.text, availableWidth, 100000f).y + topPadding + bottomPadding;
            float viewportHeight = 0f;

            if (_commentScrollRect != null && _commentScrollRect.viewport != null)
            {
                viewportHeight = _commentScrollRect.viewport.rect.height;
            }

            float targetHeight = Mathf.Max(1f, Mathf.Max(preferredHeight, viewportHeight + 1f));
            SetRectHeight(targetContent, targetHeight);

            if (textRect == targetContent)
            {
                SetRectHeight(textRect, targetHeight);
            }
        }

        private float GetCommentAvailableWidth(RectTransform textRect, RectTransform targetContent)
        {
            if (textRect != null && textRect.rect.width > 1f)
            {
                return textRect.rect.width;
            }

            if (_commentScrollRect != null && _commentScrollRect.viewport != null && _commentScrollRect.viewport.rect.width > 1f)
            {
                return _commentScrollRect.viewport.rect.width;
            }

            if (targetContent != null && targetContent.rect.width > 1f)
            {
                return targetContent.rect.width;
            }

            return 0f;
        }

        private static void SetRectHeight(RectTransform rectTransform, float height)
        {
            if (rectTransform == null)
            {
                return;
            }

            Vector2 sizeDelta = rectTransform.sizeDelta;
            sizeDelta.y = height;
            rectTransform.sizeDelta = sizeDelta;
        }

        private void ResetCommentScrollToTopImmediate()
        {
            if (_commentScrollRect == null)
            {
                return;
            }

            _commentScrollRect.StopMovement();
            _commentScrollRect.verticalNormalizedPosition = 1f;
        }

        private IEnumerator ResetCommentScrollToTopNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            if (_commentContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_commentContent);
            }

            ResetCommentScrollToTopImmediate();
            _commentScrollResetCoroutine = null;
        }

        private static void EnsureRectMask(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (target.GetComponent<RectMask2D>() != null || target.GetComponent<Mask>() != null)
            {
                return;
            }

            target.AddComponent<RectMask2D>();
        }

        private static void EnsureRaycastTargetGraphic(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            Graphic graphic = target.GetComponent<Graphic>();

            if (graphic == null)
            {
                Image image = target.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
                image.raycastTarget = true;
                return;
            }

            graphic.raycastTarget = true;
        }

        private static TextAlignmentOptions NormalizeCommentTextAlignment(TextAlignmentOptions alignment)
        {
            if ((alignment & TextAlignmentOptions.Left) == TextAlignmentOptions.Left)
            {
                return TextAlignmentOptions.TopLeft;
            }

            if ((alignment & TextAlignmentOptions.Right) == TextAlignmentOptions.Right)
            {
                return TextAlignmentOptions.TopRight;
            }

            if ((alignment & TextAlignmentOptions.Center) == TextAlignmentOptions.Center)
            {
                return TextAlignmentOptions.Top;
            }

            if ((alignment & TextAlignmentOptions.Justified) == TextAlignmentOptions.Justified)
            {
                return TextAlignmentOptions.TopJustified;
            }

            return TextAlignmentOptions.TopLeft;
        }

        private static Scrollbar FindLikelyVerticalScrollbar(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            Scrollbar[] scrollbars = root.GetComponentsInChildren<Scrollbar>(true);
            if (scrollbars == null || scrollbars.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < scrollbars.Length; i++)
            {
                Scrollbar scrollbar = scrollbars[i];
                if (scrollbar != null && scrollbar.name.IndexOf("Vertical", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return scrollbar;
                }
            }

            for (int i = 0; i < scrollbars.Length; i++)
            {
                Scrollbar scrollbar = scrollbars[i];
                if (scrollbar == null)
                {
                    continue;
                }

                RectTransform rectTransform = scrollbar.transform as RectTransform;
                if (rectTransform != null && rectTransform.rect.height >= rectTransform.rect.width)
                {
                    return scrollbar;
                }
            }

            return scrollbars[0];
        }

        private static T GetComponentInParentIncludingInactive<T>(Transform start) where T : Component
        {
            Transform current = start;

            while (current != null)
            {
                if (current.TryGetComponent(out T component))
                {
                    return component;
                }

                current = current.parent;
            }

            return null;
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (_audioSource == null || clip == null)
            {
                return;
            }

            _audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private float GetDeltaTime()
        {
            return _useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            if (!target.TryGetComponent(out CanvasGroup canvasGroup))
            {
                canvasGroup = target.AddComponent<CanvasGroup>();
            }

            return canvasGroup;
        }

        private static float EaseOutCubic(float value)
        {
            float t = Mathf.Clamp01(value);
            float inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }
    }
}
