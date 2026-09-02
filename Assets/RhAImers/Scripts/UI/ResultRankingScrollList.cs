using System.Collections.Generic;
using TMPro;
using RhAImers.Battle;
using RhAImers.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Result Ranking Scroll List")]
    public sealed class ResultRankingScrollList : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private ResultRankingRowView _rowTemplate;
        [SerializeField] private GameObject _emptyStateRoot;
        [SerializeField] private TextMeshProUGUI _emptyStateText;

        [Header("Data")]
        [SerializeField] private bool _refreshOnAwake = true;
        [SerializeField] private bool _refreshOnEnable;
        [SerializeField] private int _displayLimit;
        [SerializeField] private bool _markCurrentScore = true;
        [SerializeField] private string _currentMarkerLabel = "YOU";
        [SerializeField] private bool _showAnonymousNames;
        [SerializeField] private string _anonymousNameFormat = "PLAYER_{0:00}";
        [SerializeField] private string _emptyText = "NO DATA";

        [Header("Manual Layout")]
        [SerializeField] private bool _useManualVerticalLayout = true;
        [SerializeField] private float _rowHeight = 72f;
        [SerializeField] private float _rowSpacing = 8f;
        [SerializeField] private float _topPadding = 0f;
        [SerializeField] private float _bottomPadding = 0f;
        [SerializeField] private bool _resetScrollToTopOnRefresh = true;

        [Header("Colors")]
        [SerializeField] private bool _applyColors = true;
        [SerializeField] private Color _normalTextColor = Color.white;
        [SerializeField] private Color _normalScoreColor = new Color(0.35f, 0.9f, 1f, 1f);
        [SerializeField] private Color _normalBackgroundColor = new Color(0f, 0f, 0f, 0.18f);
        [SerializeField] private Color _currentTextColor = new Color(1f, 0.45f, 1f, 1f);
        [SerializeField] private Color _currentBackgroundColor = new Color(1f, 0.1f, 0.85f, 0.28f);
        [SerializeField] private Color _topRankColor = new Color(1f, 0.55f, 1f, 1f);

        private readonly List<ResultRankingRowView> _rows = new List<ResultRankingRowView>();
        private int _lastMarkedIndex = -1;

        private void Reset()
        {
            _scrollRect = GetComponent<ScrollRect>();

            if (_scrollRect != null)
            {
                _contentRoot = _scrollRect.content;
            }
        }

        private void Awake()
        {
            ResolveReferences();
            HideTemplate();

            if (_refreshOnAwake)
            {
                Refresh();
            }
        }

        private void OnEnable()
        {
            if (_refreshOnEnable)
            {
                Refresh();
            }
        }

        [ContextMenu("Refresh")]
        public void Refresh()
        {
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            BattleResult result = gameManager != null ? gameManager.LastBattleResult : null;
            Refresh(result, global::RankingManager.GetRanking());
        }

        public void Refresh(BattleResult result, IReadOnlyList<int> ranking)
        {
            ResolveReferences();
            HideTemplate();

            int currentScore = result != null ? result.TotalScore : -1;
            int count = ResolveDisplayCount(ranking);
            SetEmptyState(count <= 0);
            EnsureRowCount(count);
            _lastMarkedIndex = -1;

            for (int i = 0; i < _rows.Count; i++)
            {
                ResultRankingRowView row = _rows[i];
                bool visible = i < count;
                SetActive(row, visible);

                if (!visible)
                {
                    continue;
                }

                int score = ranking[i];
                bool isCurrent = ShouldMarkCurrentScore(i, score, currentScore);
                bool isTopRank = i == 0;
                string playerName = BuildAnonymousName(i + 1);

                row.SetData(
                    i + 1,
                    score,
                    playerName,
                    _showAnonymousNames,
                    isCurrent,
                    _currentMarkerLabel,
                    _applyColors,
                    isTopRank ? _topRankColor : _normalTextColor,
                    isTopRank ? _topRankColor : _normalScoreColor,
                    isCurrent ? _currentTextColor : Color.clear,
                    isCurrent ? _currentBackgroundColor : _normalBackgroundColor
                );
            }

            if (_useManualVerticalLayout)
            {
                ApplyManualLayout(count);
            }

            if (_resetScrollToTopOnRefresh && _scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                _scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void ResolveReferences()
        {
            if (_scrollRect == null)
            {
                _scrollRect = GetComponent<ScrollRect>();
            }

            if (_contentRoot == null && _scrollRect != null)
            {
                _contentRoot = _scrollRect.content;
            }
        }

        private void HideTemplate()
        {
            if (_rowTemplate != null)
            {
                _rowTemplate.gameObject.SetActive(false);
            }
        }

        private int ResolveDisplayCount(IReadOnlyList<int> ranking)
        {
            if (ranking == null || ranking.Count == 0)
            {
                return 0;
            }

            if (_displayLimit <= 0)
            {
                return ranking.Count;
            }

            return Mathf.Min(_displayLimit, ranking.Count);
        }

        private void EnsureRowCount(int count)
        {
            if (_contentRoot == null || _rowTemplate == null)
            {
                return;
            }

            while (_rows.Count < count)
            {
                ResultRankingRowView row = Instantiate(_rowTemplate, _contentRoot);
                row.gameObject.name = _rowTemplate.gameObject.name + "_" + (_rows.Count + 1);
                row.gameObject.SetActive(true);
                _rows.Add(row);
            }
        }

        private bool ShouldMarkCurrentScore(int index, int score, int currentScore)
        {
            if (!_markCurrentScore || currentScore < 0 || score != currentScore)
            {
                return false;
            }

            if (_lastMarkedIndex >= 0)
            {
                return false;
            }

            _lastMarkedIndex = index;
            return true;
        }

        private string BuildAnonymousName(int rank)
        {
            if (!_showAnonymousNames)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(_anonymousNameFormat))
            {
                return string.Empty;
            }

            return string.Format(_anonymousNameFormat, rank);
        }

        private void ApplyManualLayout(int visibleCount)
        {
            if (_contentRoot == null)
            {
                return;
            }

            float rowHeight = Mathf.Max(0f, _rowHeight);
            float rowSpacing = Mathf.Max(0f, _rowSpacing);
            float topPadding = Mathf.Max(0f, _topPadding);
            float bottomPadding = Mathf.Max(0f, _bottomPadding);

            for (int i = 0; i < visibleCount && i < _rows.Count; i++)
            {
                RectTransform rowTransform = _rows[i].RectTransform;
                if (rowTransform == null)
                {
                    continue;
                }

                rowTransform.anchorMin = new Vector2(0f, 1f);
                rowTransform.anchorMax = new Vector2(1f, 1f);
                rowTransform.pivot = new Vector2(0.5f, 1f);
                rowTransform.anchoredPosition = new Vector2(0f, -topPadding - i * (rowHeight + rowSpacing));
                rowTransform.sizeDelta = new Vector2(0f, rowHeight);
                rowTransform.localScale = Vector3.one;
            }

            float contentHeight = visibleCount > 0
                ? topPadding + visibleCount * rowHeight + (visibleCount - 1) * rowSpacing + bottomPadding
                : topPadding + bottomPadding;

            _contentRoot.anchorMin = new Vector2(0f, 1f);
            _contentRoot.anchorMax = new Vector2(1f, 1f);
            _contentRoot.pivot = new Vector2(0.5f, 1f);
            _contentRoot.sizeDelta = new Vector2(0f, Mathf.Max(0f, contentHeight));
        }

        private void SetEmptyState(bool empty)
        {
            if (_emptyStateRoot != null)
            {
                _emptyStateRoot.SetActive(empty);
            }

            if (_emptyStateText != null)
            {
                _emptyStateText.text = _emptyText;
            }
        }

        private static void SetActive(ResultRankingRowView row, bool active)
        {
            if (row != null && row.gameObject.activeSelf != active)
            {
                row.gameObject.SetActive(active);
            }
        }
    }

}
