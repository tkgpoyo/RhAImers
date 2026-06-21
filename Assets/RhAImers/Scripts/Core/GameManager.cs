using System.Collections.Generic;
using RhAImers.Battle;
using RhAImers.Scoring;
using RhAImers.VerseGeneration;

namespace RhAImers.Core
{
    /// <summary>
    /// ゲーム全体を管理するクラス
    /// </summary>
    public class GameManager
    {
        private BattleSession _currentSession;
        private IVerseGenerationService _verseGenerationService;
        private ScoreCalculator _scoreCalculator;

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
            CurrentOpponentVerse = _verseGenerationService.GenerateOpponentVerse(_currentSession.GenerateBattleContext());
        }

        public void SubmitRhymes(IReadOnlyList<string> rhymes)
        {
            CurrentState = GameState.RhymeInput;
            var playerVerse = _verseGenerationService.GeneratePlayerVerse(rhymes, CurrentOpponentVerse.Text);
            var turnData = new TurnData(_currentSession.CurrentTurnIndex, CurrentOpponentVerse, new List<string>(rhymes), playerVerse);
            _currentSession.AddTurn(turnData);
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
    }
}