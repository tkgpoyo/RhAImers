using System.Collections.Generic;
using RhAImers.Battle;
using RhAImers.Input;
using RhAImers.UI;
using UnityEngine;

namespace RhAImers.Debugging
{
    public class UIBattleDebugStarter : MonoBehaviour
    {
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private RhymeInputController _rhymeInputController;
        [SerializeField] private int _inputTimeLimitSec = 30;

        private float _remainingTime;
        private bool _isTimerRunning;

        private void OnEnable()
        {
            if (_rhymeInputController != null)
            {
                _rhymeInputController.Submitted += HandleRhymesSubmitted;
            }
        }

        private void OnDisable()
        {
            if (_rhymeInputController != null)
            {
                _rhymeInputController.Submitted -= HandleRhymesSubmitted;
            }
        }

        private void Start()
        {
            var opponentVerse = new Verse(
                "俺のライムが響くこのステージ\n君の言葉で返してみな",
                new List<VerseHighlight>()
            );

            _uiManager.ShowOpponentVerse(opponentVerse);
            _uiManager.ShowInputTimer(_inputTimeLimitSec);
            _uiManager.ShowInputRhymes(new List<string>());
            _uiManager.ShowGeneratedVerse(new Verse(string.Empty, new List<VerseHighlight>()));

            _rhymeInputController.StartInput();

            _remainingTime = _inputTimeLimitSec;
            _isTimerRunning = true;
        }

        private void Update()
        {
            if (!_isTimerRunning)
            {
                return;
            }

            int previousSec = Mathf.CeilToInt(_remainingTime);

            _remainingTime -= Time.deltaTime;

            int currentSec = Mathf.Max(0, Mathf.CeilToInt(_remainingTime));

            if (currentSec != previousSec)
            {
                _uiManager.UpdateInputTimer(currentSec);
            }

            if (_remainingTime <= 0f)
            {
                _isTimerRunning = false;
                _uiManager.UpdateInputTimer(0);

                IReadOnlyList<string> submittedRhymes = _rhymeInputController.Submit();
                HandleRhymesSubmitted(submittedRhymes);
            }
        }

        private void HandleRhymesSubmitted(IReadOnlyList<string> rhymes)
        {
            _isTimerRunning = false;

            string joinedRhymes = rhymes == null || rhymes.Count == 0
                ? "ライム未入力"
                : string.Join(" / ", rhymes);

            var generatedVerse = new Verse(
                $"入力完了\n受け取ったライム：{joinedRhymes}",
                new List<VerseHighlight>()
            );

            _uiManager.ShowGeneratedVerse(generatedVerse);
        }
    }
}