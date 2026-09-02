using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Result Ranking Row View")]
    public sealed class ResultRankingRowView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _root;
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _currentMarkerText;
        [SerializeField] private Graphic _backgroundGraphic;
        [SerializeField] private Graphic _accentGraphic;
        [SerializeField] private CanvasGroup _canvasGroup;

        public RectTransform RectTransform
        {
            get
            {
                if (_root == null)
                {
                    _root = transform as RectTransform;
                }

                return _root;
            }
        }

        private void Reset()
        {
            _root = transform as RectTransform;
            _canvasGroup = GetComponent<CanvasGroup>();
            _backgroundGraphic = GetComponent<Graphic>();
        }

        public void SetData(
            int rank,
            int score,
            string playerName,
            bool showName,
            bool isCurrentScore,
            string currentMarkerLabel,
            bool applyColors,
            Color rankColor,
            Color scoreColor,
            Color currentTextColor,
            Color backgroundColor)
        {
            if (_rankText != null)
            {
                _rankText.text = rank.ToString();
            }

            if (_scoreText != null)
            {
                _scoreText.text = score.ToString();
            }

            if (_nameText != null)
            {
                _nameText.text = showName ? playerName : string.Empty;
                _nameText.gameObject.SetActive(showName);
            }

            if (_currentMarkerText != null)
            {
                _currentMarkerText.text = currentMarkerLabel;
                _currentMarkerText.gameObject.SetActive(isCurrentScore);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            if (!applyColors)
            {
                return;
            }

            Color resolvedRankColor = isCurrentScore ? currentTextColor : rankColor;
            Color resolvedScoreColor = isCurrentScore ? currentTextColor : scoreColor;

            ApplyTextColor(_rankText, resolvedRankColor);
            ApplyTextColor(_scoreText, resolvedScoreColor);
            ApplyTextColor(_nameText, resolvedRankColor);
            ApplyTextColor(_currentMarkerText, currentTextColor);
            ApplyGraphicColor(_backgroundGraphic, backgroundColor);
            ApplyGraphicColor(_accentGraphic, resolvedScoreColor);
        }

        private static void ApplyTextColor(TextMeshProUGUI text, Color color)
        {
            if (text != null && color != Color.clear)
            {
                text.color = color;
            }
        }

        private static void ApplyGraphicColor(Graphic graphic, Color color)
        {
            if (graphic != null)
            {
                graphic.color = color;
            }
        }
    }
}
