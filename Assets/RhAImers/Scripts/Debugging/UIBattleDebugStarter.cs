using System.Collections;
using System.Collections.Generic;
using RhAImers.Battle;
using RhAImers.Input;
using RhAImers.UI;
using UnityEngine;

namespace RhAImers.Debugging
{
    public class UIBattleDebugStarter : MonoBehaviour
    {
        private static UIBattleDebugStarter _activeInstance;

        [SerializeField] private UIManager _uiManager;
        [SerializeField] private RhymeInputController _rhymeInputController;
        [SerializeField] private int _maxTurn = 3;
        [SerializeField] private int _inputTimeLimitSec = 30;
        [SerializeField] private float _nextTurnDelaySec = 1.5f;
        [SerializeField] private bool _suppressGameManagerSubmitRequested = true;

        private int _currentTurnIndex;
        private float _remainingTime;
        private bool _isWaitingForSubmit;
        private Coroutine _nextTurnCoroutine;

        private void Awake()
        {
            if (_activeInstance != null && _activeInstance != this)
            {
                Debug.LogWarning(
                    $"Duplicate UIBattleDebugStarter was disabled: {gameObject.name}. " +
                    $"Active instance is {_activeInstance.gameObject.name}."
                );

                enabled = false;
                return;
            }

            _activeInstance = this;
        }

        private void OnEnable()
        {
            if (_rhymeInputController != null)
            {
                _rhymeInputController.Submitted += HandleRhymesSubmitted;
            }

            if (_uiManager != null)
            {
                _uiManager.RetrySelected += HandleRetrySelected;
            }
        }

        private void OnDisable()
        {
            if (_rhymeInputController != null)
            {
                _rhymeInputController.Submitted -= HandleRhymesSubmitted;
                _rhymeInputController.SetNotifySubmitRequested(true);
            }

            if (_uiManager != null)
            {
                _uiManager.RetrySelected -= HandleRetrySelected;
            }

            StopNextTurnCoroutine();
            _isWaitingForSubmit = false;

            if (_activeInstance == this)
            {
                _activeInstance = null;
            }
        }

        private void Start()
        {
            if (_rhymeInputController != null && _suppressGameManagerSubmitRequested)
            {
                _rhymeInputController.SetNotifySubmitRequested(false);
            }

            RestartDebugBattle();
        }

        private void Update()
        {
            if (!_isWaitingForSubmit)
            {
                return;
            }

            int previousSec = Mathf.CeilToInt(_remainingTime);

            _remainingTime -= Time.deltaTime;

            int currentSec = Mathf.Max(0, Mathf.CeilToInt(_remainingTime));

            if (currentSec != previousSec)
            {
                _uiManager?.UpdateInputTimer(currentSec);
            }

            if (_remainingTime <= 0f)
            {
                IReadOnlyList<string> submittedRhymes = _rhymeInputController != null
                    ? _rhymeInputController.Submit()
                    : new List<string>();

                ResolveTurn(submittedRhymes);
            }
        }

        private void HandleRetrySelected()
        {
            RestartDebugBattle();
        }

        private void RestartDebugBattle()
        {
            StopNextTurnCoroutine();

            _currentTurnIndex = 0;
            _remainingTime = _inputTimeLimitSec;
            _isWaitingForSubmit = false;

            BeginTurn();
        }

        private void BeginTurn()
        {
            StopNextTurnCoroutine();

            _isWaitingForSubmit = true;
            _remainingTime = _inputTimeLimitSec;

            Verse opponentVerse = CreateOpponentVerse(_currentTurnIndex);

            _uiManager?.ShowOpponentVerse(opponentVerse);
            _uiManager?.ShowInputTimer(_inputTimeLimitSec);
            _uiManager?.ShowGeneratedVerse(new Verse(
                $"{_currentTurnIndex + 1}ターン目：ライムを入力してください",
                new List<VerseHighlight>()
            ));

            _rhymeInputController?.StartInput();
        }

        private Verse CreateOpponentVerse(int turnIndex)
        {
            string text;

            switch (turnIndex)
            {
                case 0:
                    text = "1ターン目\n俺の[[ライム]]が響くこのステージ\n君の【スタイル】で返してみな";
                    break;

                case 1:
                    text = "2ターン目\nまだまだ続くこの[[バトル]]\n次の【言葉】で流れを変えろ";
                    break;

                case 2:
                    text = "3ターン目\n最後に決めろ[[フロウ]]と[[パンチライン]]\nここで【勝負】を終わらせろ";
                    break;

                default:
                    text = $"{turnIndex + 1}ターン目\nテスト用の[[相手バース]]です";
                    break;
            }

            return new Verse(text, new List<VerseHighlight>());
        }

        private void HandleRhymesSubmitted(IReadOnlyList<string> rhymes)
        {
            ResolveTurn(rhymes);
        }

        private void ResolveTurn(IReadOnlyList<string> rhymes)
        {
            if (!_isWaitingForSubmit)
            {
                return;
            }

            _isWaitingForSubmit = false;
            _uiManager?.UpdateInputTimer(0);

            string joinedRhymes = rhymes == null || rhymes.Count == 0
                ? "ライム未入力"
                : string.Join(" / ", rhymes);

            Verse generatedVerse = new Verse(
                $"{_currentTurnIndex + 1}ターン目の入力完了\n受け取ったライム：[[{joinedRhymes}]]",
                new List<VerseHighlight>()
            );

            _uiManager?.ShowGeneratedVerse(generatedVerse);

            if (_currentTurnIndex + 1 >= _maxTurn)
            {
                ShowAllTurnsFinished();
                return;
            }

            _nextTurnCoroutine = StartCoroutine(BeginNextTurnAfterDelay());
        }

        private IEnumerator BeginNextTurnAfterDelay()
        {
            yield return new WaitForSeconds(_nextTurnDelaySec);

            _currentTurnIndex++;
            BeginTurn();

            _nextTurnCoroutine = null;
        }

        private void ShowAllTurnsFinished()
        {
            _isWaitingForSubmit = false;
            StopNextTurnCoroutine();

            _uiManager?.ShowResult(default(BattleResult));
        }

        private void StopNextTurnCoroutine()
        {
            if (_nextTurnCoroutine == null)
            {
                return;
            }

            StopCoroutine(_nextTurnCoroutine);
            _nextTurnCoroutine = null;
        }
    }
}
