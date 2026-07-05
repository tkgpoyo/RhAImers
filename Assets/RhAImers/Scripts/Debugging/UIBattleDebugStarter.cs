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

        [Header("References")]
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private RhymeInputController _rhymeInputController;

        [Header("Debug Battle")]
        [SerializeField] private int _maxTurn = 3;
        [SerializeField] private int _inputTimeLimitSec = 30;
        [SerializeField] private float _nextTurnDelaySec = 1.5f;
        [SerializeField] private float _resultDelaySec = 2.5f;
        [SerializeField] private bool _suppressGameManagerSubmitRequested = true;

        [Header("UI Presentation Wait")]
        [SerializeField] private bool _waitForUiPresentationBeforeTimer = true;

        private int _currentTurnIndex;
        private float _remainingTime;
        private bool _isWaitingForSubmit;
        private Coroutine _turnStartCoroutine;
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

            StopTurnStartCoroutine();
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
            StopTurnStartCoroutine();
            StopNextTurnCoroutine();

            _currentTurnIndex = 0;
            _remainingTime = _inputTimeLimitSec;
            _isWaitingForSubmit = false;

            BeginTurn();
        }

        private void BeginTurn()
        {
            StopTurnStartCoroutine();
            StopNextTurnCoroutine();

            _isWaitingForSubmit = false;
            _remainingTime = _inputTimeLimitSec;

            _turnStartCoroutine = StartCoroutine(BeginTurnRoutine());
        }

        private IEnumerator BeginTurnRoutine()
        {
            _uiManager?.ShowOpponentVerseLoading();
            _uiManager?.ShowInputTimer(_inputTimeLimitSec);

            if (_waitForUiPresentationBeforeTimer && _uiManager != null)
            {
                //yield return _uiManager.WaitUntilInputPresentationReady();
                yield break;        // 2026/07/05 ota コンパイル通すために仮で書いてる
            }

            _remainingTime = _inputTimeLimitSec;
            _uiManager?.ShowInputTimer(_inputTimeLimitSec);

            _rhymeInputController?.StartInput();

            _isWaitingForSubmit = true;
            _turnStartCoroutine = null;
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
                _nextTurnCoroutine = StartCoroutine(ShowResultAfterDelay());
                return;
            }

            _nextTurnCoroutine = StartCoroutine(BeginNextTurnAfterDelay());
        }

        private IEnumerator BeginNextTurnAfterDelay()
        {
            yield return new WaitForSecondsRealtime(_nextTurnDelaySec);

            _currentTurnIndex++;
            BeginTurn();

            _nextTurnCoroutine = null;
        }

        private IEnumerator ShowResultAfterDelay()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, _resultDelaySec));

            ShowAllTurnsFinished();

            _nextTurnCoroutine = null;
        }

        private void ShowAllTurnsFinished()
        {
            _isWaitingForSubmit = false;
            StopTurnStartCoroutine();

            _uiManager?.ShowResult(default(BattleResult));
        }

        private void StopTurnStartCoroutine()
        {
            if (_turnStartCoroutine == null)
            {
                return;
            }

            StopCoroutine(_turnStartCoroutine);
            _turnStartCoroutine = null;
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
