using System;
using System.Text;
using TMPro;
using RhAImers.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    [Serializable]
    public struct TurnScoreUI
    {
        public TextMeshProUGUI RhymeCountText;
        public TextMeshProUGUI AverageHardnessText;
        public TextMeshProUGUI RelevanceCountText;
        public TextMeshProUGUI TotalText;

        public void SetData(RhAImers.Scoring.TurnScore score)
        {
            if (RhymeCountText != null) RhymeCountText.text = $"{score.RhymeCount}";
            if (AverageHardnessText != null) AverageHardnessText.text = $"{score.AverageHardness:F2}";
            if (RelevanceCountText != null) RelevanceCountText.text = $"{score.RelevanceCount}";
            if (TotalText != null) TotalText.text = $"{score.Total}";
        }
    }

    public class ResultSceneManager : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _modeSelectionButton;

        [Header("Score UI")]
        [SerializeField] private TextMeshProUGUI _totalScoreText;
        [SerializeField] private TurnScoreUI _turn1ScoreUI;
        [SerializeField] private TurnScoreUI _turn2ScoreUI;
        [SerializeField] private TurnScoreUI _turn3ScoreUI;

        public event Action ModeSelectionSelected;

        private void Start()
        {
            var gameManager = FindFirstObjectByType<GameManager>();
            var result = gameManager != null ? gameManager.LastBattleResult : null;

            if (result == null)
            {
                // テスト用のモックデータを使用
                var dummyTurnScores = new System.Collections.Generic.List<RhAImers.Scoring.TurnScore>
                {
                    new RhAImers.Scoring.TurnScore(5, 0.72f, 4),
                    new RhAImers.Scoring.TurnScore(4, 0.70f, 3),
                    new RhAImers.Scoring.TurnScore(6, 0.60f, 4)
                };

                // BattleResultの生成 (TurnDataはテスト用のため空リストを渡す)
                var dummyTurns = new System.Collections.Generic.List<RhAImers.Battle.TurnData>();
                result = new RhAImers.Battle.BattleResult(dummyTurns, dummyTurnScores);
            }

            if (_totalScoreText != null)
            {
                _totalScoreText.text = $"{result.TotalScore}";
            }

            if (result.TurnScores.Count > 0)
            {
                _turn1ScoreUI.SetData(result.TurnScores[0]);
            }

            if (result.TurnScores.Count > 1)
            {
                _turn2ScoreUI.SetData(result.TurnScores[1]);
            }

            if (result.TurnScores.Count > 2)
            {
                _turn3ScoreUI.SetData(result.TurnScores[2]);
            }
        }

        private void OnEnable()
        {
            if (_modeSelectionButton != null)
            {
                _modeSelectionButton.onClick.AddListener(HandleModeSelectionButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (_modeSelectionButton != null)
            {
                _modeSelectionButton.onClick.RemoveListener(HandleModeSelectionButtonClicked);
            }
        }

        private void HandleModeSelectionButtonClicked()
        {
            ModeSelectionSelected?.Invoke();
        }
    }
}
