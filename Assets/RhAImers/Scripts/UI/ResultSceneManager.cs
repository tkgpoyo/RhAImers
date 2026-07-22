using System;
using System.Text;
using TMPro;
using RhAImers.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    public class ResultSceneManager : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _modeSelectionButton;

        [Header("Score UI (Temporary)")]
        [SerializeField] private TextMeshProUGUI _totalScoreText;
        [SerializeField] private TextMeshProUGUI _turn1ScoreText;
        [SerializeField] private TextMeshProUGUI _turn2ScoreText;
        [SerializeField] private TextMeshProUGUI _turn3ScoreText;

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
                _totalScoreText.text = $"TOTAL SCORE: {result.TotalScore * 100}";
            }

            if (_turn1ScoreText != null && result.TurnScores.Count > 0)
            {
                var t1 = result.TurnScores[0];
                _turn1ScoreText.text = $"TURN 1\n韻の個数: {t1.RhymeCount} / 韻の硬さ: {t1.AverageHardness:F2} / 関連度ボーナス: x{t1.RelevanceCount}\nTurn Total: {t1.Total}";
            }

            if (_turn2ScoreText != null && result.TurnScores.Count > 1)
            {
                var t2 = result.TurnScores[1];
                _turn2ScoreText.text = $"TURN 2\n韻の個数: {t2.RhymeCount} / 韻の硬さ: {t2.AverageHardness:F2} / 関連度ボーナス: x{t2.RelevanceCount}\nTurn Total: {t2.Total}";
            }

            if (_turn3ScoreText != null && result.TurnScores.Count > 2)
            {
                var t3 = result.TurnScores[2];
                _turn3ScoreText.text = $"TURN 3\n韻の個数: {t3.RhymeCount} / 韻の硬さ: {t3.AverageHardness:F2} / 関連度ボーナス: x{t3.RelevanceCount}\nTurn Total: {t3.Total}";
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
