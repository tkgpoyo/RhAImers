using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using RhAImers.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    public class UIManager : MonoBehaviour
    {
        private const string HighlightColor = "#FFD54F";

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

        public event Action RetrySelected;

        private void Awake()
        {
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
            SetStatus("Opponent Verse");
            SetResultVisible(false);

            string text = verse == null ? string.Empty : FormatVerseText(verse.Text);
            SetText(_opponentVerseText, text);
        }

        public void ShowInputTimer(int sec)
        {
            UpdateInputTimer(sec);
        }

        public void UpdateInputTimer(int sec)
        {
            SetText(_inputTimerText, FormatTimer(sec));
        }

        public void ShowInputRhymes(IReadOnlyList<string> rhymes)
        {
            if (_inputRhymesText == null)
            {
                Debug.LogError("InputRhymesText is not assigned in UIManager.");
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

        public void ShowGeneratedVerse(Verse verse)
        {
            SetStatus("Generated Verse");
            SetResultVisible(false);

            string text = verse == null ? string.Empty : FormatVerseText(verse.Text);
            SetText(_generatedVerseText, text);
        }

        public void ShowResult(BattleResult result)
        {
            SetStatus("Result");
            SetResultVisible(true);

            string resultText = BuildResultText(result);
            SetText(_resultText, resultText);
        }

        public void ShowOpponentVerseLoading()
        {
            SetStatus("Opponent Verse Loading");
            SetResultVisible(false);
            SetText(_opponentVerseText, "相手のバース生成中...");
        }

        public void ShowGenerationLoading()
        {
            SetStatus("Verse Generation Loading");
            SetResultVisible(false);
            SetText(_generatedVerseText, "あなたのバース生成中...");
        }

        public void ShowScoringLoading()
        {
            SetStatus("Scoring Loading");
            SetResultVisible(false);
            SetText(_resultText, "採点中...");
        }

        private void HandleRetryButtonClicked()
        {
            RetrySelected?.Invoke();
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

        private string EscapeRichText(string text)
        {
            return text
                .Replace("<", "＜")
                .Replace(">", "＞");
        }
    }
}
