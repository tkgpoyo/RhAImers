using System;
using System.Collections.Generic;
using System.Collections;
using RhAImers.Battle;
using RhAImers.Input;
using RhAImers.Scoring;
using RhAImers.UI;
using RhAImers.VerseGeneration;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace RhAImers.Core
{
    /// <summary>
    /// ゲーム全体を管理するクラス
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>最大ターン数</summary>
        /// <remarks>TODO: 将来的に削除する</remarks>
        private const int MAX_TURN = 3;

        /// <summary>バース生成を行うサービス</summary>
        private IVerseGenerationService _verseGenerationService;
        /// <summary>得点計算を行うクラス</summary>
        private ScoreCalculator _scoreCalculator;
        /// <summary>入力タイマーのキャンセルトークンソース</summary>
        private CancellationTokenSource _cancellationTokenSource = new();

        [SerializeField] private RhymeInputController _rhymeInputController;
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private int _defaultInputTimeLimitSec = 30;

        private Coroutine _inputTimerCoroutine;
        private bool _hasSubmittedCurrentTurn;

        public GameState CurrentState { get; private set; }
        public BattleSettings CurrentSettings { get; private set; }

        private void Awake()
        {
            _verseGenerationService = new FixedVerseGenerationService();                                                                    // TODO: これでいいの？
            _scoreCalculator = new ScoreCalculator(new(new()), new(new(LlmClient.API_KEY_SAMPLE), new(new RhymeDictionary(new()))));        // TODO: 仮実装のためちゃんと実装

            StartGame();
        }

        private void Update()
        {
            switch (CurrentState) {
                case GameState.Title:
                    break;
                case GameState.ModeSelect:
                    break;
                case GameState.BattleStart:
                    UpdateBattleStart();
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

        #region Updateメソッド内の処理
        #region UpdateBattleStart：BattleStart状態の更新処理
        private void UpdateBattleStart()
        {
        }
        #endregion (UpdateBattleStart)

        #region UpdateOpponentVerse：OpponentVerse状態の更新処理
        private void UpdateOpponentVerse()
        {
        }
        #endregion (UpdateOpponentVerse)

        #region UpdateRhymeInput：RhymeInput状態の更新処理
        private void UpdateRhymeInput()
        {
        }
        #endregion (UpdateRhymeInput)

        #region UpdateVerseGeneration：VerseGeneration状態の更新処理
        private void UpdateVerseGeneration()
        {
        }
        #endregion (UpdateVerseGeneration)

        #region UpdateTurnEnd：TurnEnd状態の更新処理
        private void UpdateTurnEnd()
        {
        }
        #endregion (UpdateTurnEnd)
        #endregion (Updateメソッド内の処理)

        /// <summary>
        /// ゲームを開始します．
        /// </summary>
        private void StartGame()
        {
            // TODO: 仮実装から本実装にする必要がある
            CurrentSettings = new BattleSettings(MAX_TURN, _defaultInputTimeLimitSec, Difficulty.Normal);
            RunBattleAsync(CurrentSettings).Forget();
        }

        /// <summary>
        /// バトルを非同期で実行します．
        /// </summary>
        /// <returns></returns>
        private async UniTask RunBattleAsync(BattleSettings settings)
        {
            // バトル開始
            CurrentState = GameState.BattleStart;
            var currentSession = new BattleSession(MAX_TURN);                                           // バトルセッションの生成 TODO: MaxTurnを設定から取得するようにする

            for (int turn = 0; turn < settings.MaxTurn; turn++) {
                // セットアップ
                var currentContext = currentSession.GenerateBattleContext();                            // バトルコンテキストの生成

                // 相手バース生成
                CurrentState = GameState.OpponentVerse;                                                 // ゲーム状態を「相手バース生成中」に変更
                _uiManager.ShowOpponentVerseLoading();                                                  // 相手バース生成中のUI表示
                var opponentVerse = await _verseGenerationService.GenerateOpponentVerseAsync(
                    currentContext, 
                    _cancellationTokenSource.Token
                );                                                                                      // 相手バースの取得
                _uiManager.ShowOpponentVerse(opponentVerse);                                            // 相手バースの表示

                // ライム入力
                CurrentState = GameState.RhymeInput;
                _rhymeInputController.StartInput();                                                     // ライム入力の開始
                await InputTimer(settings.InputTimeLimitSec, _cancellationTokenSource.Token);           // 入力タイマー
                var submittedRhymes = _rhymeInputController.Submit();                                   // 入力されたライムの取得

                // プレイヤーバースの生成
                CurrentState = GameState.VerseGeneration;
                _uiManager.ShowGenerationLoading();                                                     // プレイヤーバース生成中のUI表示
                var playerVerse = await _verseGenerationService.GeneratePlayerVerseAsync(
                    submittedRhymes,
                    opponentVerse.Text,
                    _cancellationTokenSource.Token
                );                                                                                      // プレイヤーバースの取得
                _uiManager.ShowGeneratedVerse(playerVerse);                                             // プレイヤーバースの表示

                // ターンデータの追加
                CurrentState = GameState.TurnEnd;
                var turnData = new TurnData(
                    turn,
                    opponentVerse,
                    new List<string>(submittedRhymes),
                    playerVerse
                );                                                                                      // ターンデータの生成
                currentSession.AddTurn(turnData);                                                       // ターンデータの追加
            }

            // 得点の計算
            CurrentState = GameState.Scoring;
            _uiManager.ShowScoringLoading();                                                            // 得点計算中のUI表示
            var scores = _scoreCalculator.Calculate(currentSession.Turns);                              // TODO: 非同期のほうがいい

            // 結果の表示
            CurrentState = GameState.Result;
            var result = new BattleResult(currentSession.Turns, scores);                                // 結果データの生成
            _uiManager.ShowResult(result);                                                              // 結果の表示
        }

        /// <summary>
        /// 指定された秒数の入力タイマーを非同期で実行します．
        /// その際，UIの残り時間表示を更新します．
        /// </summary>
        /// <param name="sec">指定秒数</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns></returns>
        private async UniTask InputTimer(int sec, CancellationToken ct = default)
        {
            var remainingSec = Mathf.Max(0, sec);       // 残り時間（int）
            var elapsedMillisec = 0f;                   // 経過時間
            _uiManager.ShowInputTimer(remainingSec);    // 残り時間の表示

            while (remainingSec > 0 && !_hasSubmittedCurrentTurn && !ct.IsCancellationRequested) {
                await UniTask.Yield(ct);                                                    // 1フレーム待機
                elapsedMillisec += Time.deltaTime * 1000;                                   // 経過時間をミリ秒で加算

                // 残り時間（int）が更新されたときにUIを更新
                var newRemainingSec = Mathf.Max(0, sec - (int)(elapsedMillisec / 1000));    // 残り時間を秒単位で計算
                if (newRemainingSec != remainingSec) {                                      // 残り時間が変化した場合
                    remainingSec = newRemainingSec;                                         // 残り時間を更新
                    _uiManager.UpdateInputTimer(remainingSec);                              // UIを更新
                }
            }
        }
    }
}