using System;
using System.Collections.Generic;
using System.Text;
using RhAImers.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Battle UI")]
        [SerializeField] private Text _opponentVerseText;
        [SerializeField] private Text _inputTimerText;
        [SerializeField] private Text _inputRhymesText;
        [SerializeField] private Text _generatedVerseText;
        [SerializeField] private Text _resultText;
        [SerializeField] private Text _statusText;

        /// ADD 2026/06/26 yota リトライ機能実装のため
        /// <summary>リトライボタンが選択されたときのイベント</summary>
        public event Action OnRetrySelected;

        public void ShowTitle()
        {
            SetStatus("Title");
        }

        public void ShowModeSelect()
        {
            SetStatus("Mode Select");
        }


        public void ShowOpponentVerse(Verse verse)
        {
            if (_opponentVerseText == null)
            {
                Debug.LogError("OpponentVerseText is not assigned.");
                return;
            }

            string text = verse == null ? string.Empty : verse.Text;
            _opponentVerseText.text = text;

            Debug.Log($"ShowOpponentVerse: {text}");
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
            Debug.Log($"ShowInputRhymes called. count={(rhymes == null ? -1 : rhymes.Count)}");

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

            Debug.Log($"InputRhymesText updated: {_inputRhymesText.text}");
        }

        public void ShowGeneratedVerse(Verse verse)
        {
            SetStatus("Generated Verse");

            string text = verse == null ? string.Empty : verse.Text;
            SetText(_generatedVerseText, text);
        }

        public void ShowResult(BattleResult result)
        {
            SetStatus("Result");

            object resultObject = result;
            string text = resultObject?.ToString() ?? string.Empty;

            SetText(_resultText, text);
        }

        public void ShowOpponentVerseLoading()
        {
            SetStatus("Opponent Verse Loading");
            SetText(_opponentVerseText, "相手のバース生成中...");
        }

        public void ShowGenerationLoading()
        {
            SetStatus("Verse Generation Loading");
            SetText(_generatedVerseText, "あなたのバース生成中...");
        }

        public void ShowScoringLoading()
        {
            SetStatus("Scoring Loading");
            SetText(_resultText, "採点中...");
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

            target.text = value;
        }

        private string FormatTimer(int sec)
        {
            int clampedSec = Mathf.Max(0, sec);
            int minutes = clampedSec / 60;
            int seconds = clampedSec % 60;

            return $"{minutes:00}:{seconds:00}";
        }
    }
}