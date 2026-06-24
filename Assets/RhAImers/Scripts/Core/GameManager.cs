using System;
using System.Collections.Generic;
using Codice.Client.Common.GameUI;
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
        /// <summary>今のターンのバトルコンテキスト</summary>
        private BattleContext _currentContext;
        private IVerseGenerationService _verseGenerationService;
        private ScoreCalculator _scoreCalculator;
        /// <summary>ライム入力残り時間(msec)</summary>
        private float _remainRhymeInputTimeMs;
        /// <summary>入力されたライムのリスト</summary>
        private IReadOnlyList<string> _submittedRhymes;

        [SerializeField] private RhymeInputController _rhymeInputController;
        [SerializeField] private UIManager _uiManager;

        public GameState CurrentState { get; private set; }
        public Verse CurrentOpponentVerse { get; private set; }
        public BattleSettings CurrentSettings { get; private set; }

        private void Awake()
        {
            _verseGenerationService = new FixedVerseGenerationService();
            _scoreCalculator = new ScoreCalculator(new(new()), new(new(), new(new RhymeDictionary(new()))));        // TODO: 仮実装のためちゃんと実装
            CurrentState = GameState.Title;
        }

        private void Update()
        {
            switch (CurrentState) {
                case GameState.Title:
                    break;
                case GameState.ModeSelect:
                    break;
                case GameState.BattleStart:
                    break;
                case GameState.OpponentVerse:
                    UpdateOpponentVerse();
                    break;
                case GameState.RhymeInput:
                    UpdateRhymeInput();
                    break;
                case GameState.VerseGeneration:
                    UpdateVerseGeneration();
                    break;
                case GameState.TurnEnd:
                    UpdateTurnEnd();
                    break;
                case GameState.Scoring:
                    break;
                case GameState.Result:
                    break;
            }
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
            // 相手バースの生成
            _currentContext = _currentSession.GenerateBattleContext();                                  // バトルコンテキストの生成
            CurrentState = GameState.OpponentVerse;                                                     // ゲーム状態を「相手バース生成中」に変更
            CurrentOpponentVerse = _verseGenerationService.GenerateOpponentVerse(_currentContext);      // 相手バースを生成する
            _uiManager.ShowOpponentVerse(CurrentOpponentVerse);                                         // 相手バースを表示

            // ライムの入力受付開始
            _uiManager.ShowInputTimer(CurrentSettings.InputTimeLimitSec);                               // ライム入力タイマーを表示
            _remainRhymeInputTimeMs = CurrentSettings.InputTimeLimitSec * 1000;                         // ライム入力残り時間を初期化
            _rhymeInputController.StartInput();                                                         // ライムの入力受付開始
            CurrentState = GameState.RhymeInput;                                                        // ゲーム状態を「ライム入力中」に変更
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
            if (_currentSession.IsFinalTurn()) {
                CurrentState = GameState.Result;
            }
            else {
                CurrentState = GameState.TurnEnd;
            }
        }

        public void EndBattle()
        {
            CurrentState = GameState.Result;
        }

        #region Updateメソッド内の処理
        private void UpdateOpponentVerse()
        {
            // 相手バースの生成が完了したら，ライム入力受付に移行する
            CurrentState = GameState.RhymeInput;
        }
        private void UpdateRhymeInput()
        {
            _remainRhymeInputTimeMs -= Time.deltaTime * 1000;
            if (_remainRhymeInputTimeMs <= 0) {
                _submittedRhymes = _rhymeInputController.Submit();
                CurrentState = GameState.VerseGeneration;
            }
        }
        private void UpdateVerseGeneration()
        {
            var playerVerse = _verseGenerationService.GeneratePlayerVerse(_submittedRhymes, CurrentOpponentVerse.Text);
            var turnData = new TurnData(_currentSession.CurrentTurnIndex, CurrentOpponentVerse, new List<string>(_submittedRhymes), playerVerse);
            _currentSession.AddTurn(turnData);
            _uiManager.ShowGeneratedVerse(playerVerse);
            CurrentState = GameState.TurnEnd;
        }
        private void UpdateTurnEnd()
        {
            if (_currentSession.IsFinalTurn()) {
                CurrentState = GameState.Result;
            }
            else {
                _uiManager.ShowScoringLoading();
                var scores = _scoreCalculator.Calculate(_currentSession.Turns);
                var result = new BattleResult(_currentSession.Turns, scores);
                _uiManager.ShowResult(result);
                CurrentState = GameState.OpponentVerse;
            }
        }
        #endregion (Updateメソッド内の処理)
    }
}