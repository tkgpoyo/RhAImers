using System.Collections.Generic;
using System.Collections;
using RhAImers.Battle;
using RhAImers.Input;
using RhAImers.Scoring;
using RhAImers.UI;
using RhAImers.VerseGeneration;
using UnityEngine;

namespace RhAImers.Core
{
    /// <summary>
    /// ゲーム全体を管理するクラス
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private BattleSession _currentSession;
        private IVerseGenerationService _verseGenerationService;
        private ScoreCalculator _scoreCalculator;

        [SerializeField] private RhymeInputController _rhymeInputController;
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private int _defaultInputTimeLimitSec = 30;

        private Coroutine _inputTimerCoroutine;
        private bool _hasSubmittedCurrentTurn;

        public GameState CurrentState { get; private set; }
        public Verse CurrentOpponentVerse { get; private set; }
        public BattleSettings CurrentSettings { get; private set; }

        public GameManager(IVerseGenerationService verseGenerationService, ScoreCalculator scoreCalculator)
        {
            _verseGenerationService = verseGenerationService;
            _scoreCalculator = scoreCalculator;
            CurrentState = GameState.Title;
        }

        public void StartGame()
        {
            CurrentState = GameState.ModeSelect;
        }

        public void StartBattle(BattleSettings settings)
        {
            CurrentSettings = settings;
            _currentSession = new BattleSession(settings.MaxTurn);
            CurrentState = GameState.BattleStart;
        }

        public void StartTurn()
        {
            CurrentState = GameState.OpponentVerse;

            _uiManager?.ShowOpponentVerseLoading();

            CurrentOpponentVerse = _verseGenerationService.GenerateOpponentVerse(
                _currentSession.GenerateBattleContext()
            );

            _uiManager?.ShowOpponentVerse(CurrentOpponentVerse);

            CurrentState = GameState.RhymeInput;
            _hasSubmittedCurrentTurn = false;

            _rhymeInputController?.StartInput();
            StartInputTimer(_defaultInputTimeLimitSec);
        }

        public void SubmitRhymes(IReadOnlyList<string> rhymes)
        {
            if (_hasSubmittedCurrentTurn)
            {
                return;
            }

            _hasSubmittedCurrentTurn = true;
            StopInputTimer();

            _uiManager?.ShowGenerationLoading();

            CurrentState = GameState.RhymeInput;

            var playerVerse = _verseGenerationService.GeneratePlayerVerse(
                rhymes,
                CurrentOpponentVerse.Text
            );

            var turnData = new TurnData(
                _currentSession.CurrentTurnIndex,
                CurrentOpponentVerse,
                new List<string>(rhymes),
                playerVerse
            );

            _currentSession.AddTurn(turnData);

            _uiManager?.ShowGeneratedVerse(playerVerse);
        }

        public void EndTurn()
        {
            if (_currentSession.IsFinalTurn())
            {
                CurrentState = GameState.Result;
            }
            else
            {
                CurrentState = GameState.TurnEnd;
            }
        }

        public void EndBattle()
        {
            CurrentState = GameState.Result;
        }

        private void OnEnable()
        {
            if (_rhymeInputController != null)
            {
                _rhymeInputController.Submitted += SubmitRhymes;
            }
        }

        private void OnDisable()
        {
            if (_rhymeInputController != null)
            {
                _rhymeInputController.Submitted -= SubmitRhymes;
            }

            StopInputTimer();
        }

        private void StartInputTimer(int sec)
        {
            StopInputTimer();
            _inputTimerCoroutine = StartCoroutine(InputTimerCoroutine(sec));
        }

        private void StopInputTimer()
        {
            if (_inputTimerCoroutine == null)
            {
                return;
            }

            StopCoroutine(_inputTimerCoroutine);
            _inputTimerCoroutine = null;
        }

        private IEnumerator InputTimerCoroutine(int sec)
        {
            int remainingSec = Mathf.Max(0, sec);

            _uiManager?.ShowInputTimer(remainingSec);

            while (remainingSec > 0 && !_hasSubmittedCurrentTurn)
            {
                yield return new WaitForSeconds(1f);

                remainingSec--;
                _uiManager?.UpdateInputTimer(remainingSec);
            }

            if (_hasSubmittedCurrentTurn)
            {
                yield break;
            }

            IReadOnlyList<string> rhymes = _rhymeInputController != null
                ? _rhymeInputController.Submit()
                : new List<string>();

            SubmitRhymes(rhymes);
        }
    }
}