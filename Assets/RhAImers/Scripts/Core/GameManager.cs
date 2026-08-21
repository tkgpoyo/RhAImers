using System;
using System.Collections.Generic;
using System.Collections;
using RhAImers.Battle;
using RhAImers.Input;
using RhAImers.Scoring;
using RhAImers.UI;
using RhAImers.VerseGeneration;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using System.Threading;
using RhAImers.AvatorMotion;
using System.Linq;

namespace RhAImers.Core
{
    /// <summary>
    /// ゲーム全体を管理するクラス．
    /// シーンをまたいで永続化し，各シーンのUIマネージャーが発行するボタンイベントを購読して，
    /// シーン遷移を含むゲーム進行を一元的に管理します．
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private const string TitleSceneName = "TitleScene";
        private const string ModeSelectionSceneName = "ModeSelectionScene";
        private const string BattleSceneName = "RhAImers0624";
        private const string ResultSceneName = "ResultScene";

        private const string RANKING_KEY = "Ranking";

        /// <summary>最大ターン数</summary>
        /// <remarks>TODO: 将来的に削除する</remarks>
        private const int MAX_TURN = 3;

        private static GameManager _instance;

        /// <summary>バース生成を行うサービス</summary>
        private IVerseGenerationService _verseGenerationService;
        /// <summary>得点計算を行うクラス</summary>
        private ScoreCalculator _scoreCalculator;
        /// <summary>入力タイマーのキャンセルトークンソース</summary>
        private CancellationTokenSource _inputRhymeCts = new();
        /// <summary>LLMクライアント</summary>
        private LlmClient _llmClient;

        private RhymeInputController _rhymeInputController;
        private UIManager _uiManager;
        private StartSceneManager _startSceneManager;
        private ModeSelectionManager _modeSelectionManager;
        private RapperMotionController _rapperMotionController;
        private ResultSceneManager _resultSceneManager;

        [SerializeField] private int _defaultInputTimeLimitSec = 30;

        /// <summary>モード選択画面で選択された難易度</summary>
        private Difficulty _selectedDifficulty = Difficulty.Normal;

        public GameState CurrentState { get; private set; }
        public BattleSettings CurrentSettings { get; private set; }
        public BattleResult LastBattleResult { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _llmClient = LlmClient.CreateFromEnvironment();
            //_verseGenerationService = new LlmVerseGenerationService(_llmClient, new(new RhymeDictionary(new())));       // TODO: 仮実装のためちゃんと実装
            //_scoreCalculator = new ScoreCalculator(new(new()), new(_llmClient, new(new RhymeDictionary(new()))));       // TODO: 仮実装のためちゃんと実装
            var rhymeDictionary = RhymeDictionaryLoader.LoadFromResources();                                            // ライム辞書
            //_verseGenerationService = new LlmVerseGenerationService(_llmClient, new(rhymeDictionary));                  // バース生成サービス
            _verseGenerationService = new MultipleLlmVerseGenerationService(_llmClient, new(rhymeDictionary));                  // バース生成サービス
            _scoreCalculator = new ScoreCalculator(new(new(), _llmClient, new(rhymeDictionary)), new(_llmClient, new(rhymeDictionary)));                  // 得点計算クラス
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnsubscribeStartScene();
            UnsubscribeModeSelection();
            UnsubscribeBattle();
            UnsubscribeResultScene();
        }

        /// <summary>
        /// シーンがロードされた際に，そのシーンに対応するUIマネージャーを解決し，
        /// ボタンイベントの購読を張り替えます．
        /// </summary>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_instance != this)
            {
                return;
            }

            UnsubscribeStartScene();
            UnsubscribeModeSelection();
            UnsubscribeBattle();
            UnsubscribeResultScene();

            switch (scene.name)
            {
                case TitleSceneName:
                    CurrentState = GameState.Title;
                    _startSceneManager = FindFirstObjectByType<StartSceneManager>();

                    if (_startSceneManager != null)
                    {
                        _startSceneManager.StartSelected += HandleStartSceneStartSelected;
                    }

                    break;

                case ModeSelectionSceneName:
                    CurrentState = GameState.ModeSelect;
                    _modeSelectionManager = FindFirstObjectByType<ModeSelectionManager>();

                    if (_modeSelectionManager != null)
                    {
                        _modeSelectionManager.StartSelected += HandleModeSelectionStartSelected;
                        _modeSelectionManager.BackSelected += HandleModeSelectionBackSelected;
                        _modeSelectionManager.DifficultySelected += HandleModeSelectionDifficultySelected;
                    }

                    break;

                case BattleSceneName:
                    _rhymeInputController = FindFirstObjectByType<RhymeInputController>();
                    _uiManager = FindFirstObjectByType<UIManager>();
                    _rapperMotionController = FindFirstObjectByType<RapperMotionController>();

                    if (_rhymeInputController != null)
                    {
                        _rhymeInputController.SubmitRequested += HandleSubmitRequested;
                    }

                    if (_uiManager != null)
                    {
                        _uiManager.ResultSelected += HandleResultSelected;
                    }


                    StartGame();
                    break;

                case ResultSceneName:
                    CurrentState = GameState.Result;
                    _resultSceneManager = FindFirstObjectByType<ResultSceneManager>();

                    if (_resultSceneManager != null)
                    {
                        _resultSceneManager.ModeSelectionSelected += HandleResultSceneModeSelectionSelected;
                    }

                    break;
            }
        }

        private void UnsubscribeStartScene()
        {
            if (_startSceneManager != null)
            {
                _startSceneManager.StartSelected -= HandleStartSceneStartSelected;
                _startSceneManager = null;
            }
        }

        private void UnsubscribeModeSelection()
        {
            if (_modeSelectionManager != null)
            {
                _modeSelectionManager.StartSelected -= HandleModeSelectionStartSelected;
                _modeSelectionManager.BackSelected -= HandleModeSelectionBackSelected;
                _modeSelectionManager.DifficultySelected -= HandleModeSelectionDifficultySelected;
                _modeSelectionManager = null;
            }
        }

        private void UnsubscribeBattle()
        {
            if (_rhymeInputController != null)
            {
                _rhymeInputController.SubmitRequested -= HandleSubmitRequested;
                _rhymeInputController = null;
            }

            if (_uiManager != null)
            {
                _uiManager.ResultSelected -= HandleResultSelected;
                _uiManager = null;
            }

            _rapperMotionController = null;
        }

        private void UnsubscribeResultScene()
        {
            if (_resultSceneManager != null)
            {
                _resultSceneManager.ModeSelectionSelected -= HandleResultSceneModeSelectionSelected;
                _resultSceneManager = null;
            }
        }

        /// <summary>
        /// <see cref="StartSceneManager.StartSelected"/>イベントのイベントハンドラ
        /// </summary>
        private void HandleStartSceneStartSelected()
        {
            SceneManager.LoadScene(ModeSelectionSceneName);
        }

        /// <summary>
        /// <see cref="ModeSelectionManager.StartSelected"/>イベントのイベントハンドラ
        /// </summary>
        private void HandleModeSelectionStartSelected()
        {
            SceneManager.LoadScene(BattleSceneName);
        }

        /// <summary>
        /// <see cref="ModeSelectionManager.BackSelected"/>イベントのイベントハンドラ
        /// </summary>
        private void HandleModeSelectionBackSelected()
        {
            SceneManager.LoadScene(TitleSceneName);
        }

        /// <summary>
        /// <see cref="ModeSelectionManager.DifficultySelected"/>イベントのイベントハンドラ
        /// </summary>
        private void HandleModeSelectionDifficultySelected(Difficulty difficulty)
        {
            _selectedDifficulty = difficulty;
        }

        /// <summary>
        /// <see cref="ResultSceneManager.ModeSelectionSelected"/>イベントのイベントハンドラ
        /// </summary>
        private void HandleResultSceneModeSelectionSelected()
        {
            SceneManager.LoadScene(ModeSelectionSceneName);
        }

        private void Update()
        {
            // MEMO: あんまり使わないかも？キャンセル処理関連で使うかもしれない
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

        #region イベント処理
        /// <summary>
        /// <see cref="RhymeInputController.SubmitRequested"/>イベントのイベントハンドラ
        /// </summary>
        private void HandleSubmitRequested()
        {
            if (CurrentState is GameState.RhymeInput) { // ライム入力中の場合
                _inputRhymeCts?.Cancel();               // 入力タイマーのキャンセル
            }
        }
        /// <summary>
        /// <see cref="UIManager.ResultSelected">イベントのイベントハンドラ
        /// </summary>
        private void HandleResultSelected()
        {
            // TODO: 雑かも
            SceneManager.LoadScene(ResultSceneName);
            //if (CurrentState is GameState.Result)
            //{
                //// TODO: リセット処理必要かも？
                //StartGame();                            // ゲーム開始
            //}
        }
        #endregion (イベント処理)

        /// <summary>
        /// ゲームを開始します．
        /// </summary>
        private void StartGame()
        {
            // TODO: 仮実装から本実装にする必要がある
            CurrentSettings = new BattleSettings(MAX_TURN, _defaultInputTimeLimitSec, _selectedDifficulty);
            RunBattleAsync(CurrentSettings).Forget(ex => Debug.LogException(ex));
        }

        /// <summary>
        /// バトルを非同期で実行します．
        /// </summary>
        /// <returns></returns>
        private async UniTask RunBattleAsync(BattleSettings settings)
        {
            // バトル開始
            CurrentState = GameState.BattleStart;
            var currentSession = new BattleSession(settings.MaxTurn);                                   // バトルセッションの生成 TODO: MaxTurnを設定から取得するようにする

            await _uiManager.ShowBattleStartSignalAsync();                                              // バトル開始のUI表示

            for (int turn = 0; turn < settings.MaxTurn; turn++) {
                // セットアップ
                var currentContext = currentSession.GenerateBattleContext();                            // バトルコンテキストの生成

                // 相手バース生成
                var cts = new CancellationTokenSource();
                CurrentState = GameState.OpponentVerse;                                                 // ゲーム状態を「相手バース生成中」に変更
                _uiManager.ShowOpponentVerseLoading();                                                  // 相手バース生成中のUI表示
                var opponentVerse = await GenerateWithFallbackAsync(
                    (service, token) => service.GenerateOpponentVerseAsync(currentContext, token),
                    cts.Token
                );                                                                                      // 相手バースの取得

                // TODO: 雑な Dispose処理
                cts.Dispose();
                cts = null;

                await _uiManager.ShowOpponentVerseAsync(opponentVerse);                                            // 相手バースの表示

                // ライム入力
                cts = new CancellationTokenSource();
                _inputRhymeCts = cts;
                CurrentState = GameState.RhymeInput;
                try {
                    _rhymeInputController.StartInput();                                                 // ライム入力の開始
                    await InputTimer(settings.InputTimeLimitSec, _uiManager, _inputRhymeCts.Token);     // 入力タイマー
                }
                catch {
                    // TODO: 例外処理が必要...？
                }
                finally {
                    cts.Dispose();                                                                      // キャンセルトークンソースの破棄
                    if (_inputRhymeCts == cts) {
                        _inputRhymeCts = null;
                    }
                }
                var submittedRhymes = _rhymeInputController.Submit();                                   // 入力されたライムの取得

                // プレイヤーバースの生成
                cts = new CancellationTokenSource();
                CurrentState = GameState.VerseGeneration;
                _uiManager.ShowGenerationLoading();                                                     // プレイヤーバース生成中のUI表示
                var playerVerse = await GenerateWithFallbackAsync(
                    (service, token) => service.GeneratePlayerVerseAsync(submittedRhymes, opponentVerse.Text, token),
                    cts.Token
                );

                // TODO: 雑な Dispose処理
                cts.Dispose();
                cts = null;

                await _uiManager.ShowGeneratedVerseAsync(playerVerse);                                             // プレイヤーバースの表示

                // やられモーション
                await _rapperMotionController.CombatMotionAsync();
                // 元のラップモーションに戻す
                _rapperMotionController.StartRapMotion();

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
            var scores = await _scoreCalculator.CalculateAsync(currentSession.Turns);                   // 得点を取得

            // ADD 2026/08/21 ota 得点の記録
            UpdateRanking(scores.Sum(score => score.Total));

            //// 結果の表示
            //CurrentState = GameState.Result;
            LastBattleResult = new BattleResult(currentSession.Turns, scores);                                // 結果データの保存
            _uiManager.ShowResult(LastBattleResult);                                                              // 結果の表示

            //SceneManager.LoadScene(ResultSceneName);
        }

        /// <summary>
        /// 指定された秒数の入力タイマーを非同期で実行します．
        /// その際，UIの残り時間表示を更新します．
        /// </summary>
        /// <param name="sec">指定秒数</param>
        /// <param name="ct">キャンセルトークン</param>
        /// <returns></returns>
        private async UniTask InputTimer(int sec, UIManager uIManager, CancellationToken ct = default)
        {
            var remainingSec = Mathf.Max(0, sec);       // 残り時間（int）
            var elapsedMillisec = 0f;                   // 経過時間
            uIManager.ShowInputTimer(remainingSec);    // 残り時間の表示

            while (remainingSec > 0 && !ct.IsCancellationRequested) {
                await UniTask.Yield(ct);                                                    // 1フレーム待機
                elapsedMillisec += Time.deltaTime * 1000;                                   // 経過時間をミリ秒で加算

                // 残り時間（int）が更新されたときにUIを更新
                var newRemainingSec = Mathf.Max(0, sec - (int)(elapsedMillisec / 1000));    // 残り時間を秒単位で計算
                if (newRemainingSec != remainingSec) {                                      // 残り時間が変化した場合
                    remainingSec = newRemainingSec;                                         // 残り時間を更新
                    uIManager.UpdateInputTimer(remainingSec);                              // UIを更新
                }
            }
        }

        private async UniTask<Verse> GenerateWithFallbackAsync(Func<IVerseGenerationService, CancellationToken, UniTask<Verse>> generate, CancellationToken ct)
        {
            try {
                return await generate(_verseGenerationService, ct);
            }
            catch {
                // TODO: API系のエラーのみを掴む
                _verseGenerationService = GetVerseGenerationService(VerseGenerationType.Fixed);
                return await generate(_verseGenerationService, ct);
            }

            /// <summary>
            /// バース生成方法を指定して、対応する<see cref="IVerseGenerationService"/>を取得します。
            /// </summary>
            IVerseGenerationService GetVerseGenerationService(VerseGenerationType verseGenerationType)
            {
                return verseGenerationType switch
                {
                    VerseGenerationType.Fixed => new FixedVerseGenerationService(),
                    _ => throw new NotImplementedException(),
                };
            }
        }

        // ADD 2026/08/21 ota ランキング機能
        private void UpdateRanking(int score)
        {
            var sRanking = PlayerPrefs.GetString(RANKING_KEY);
            List<int> ranking = sRanking.Split(',')
                                        .Where(sScore => int.TryParse(sScore, out _))
                                        .Select(sScore => int.Parse(sScore)).ToList();
            ranking.Add(score);
            ranking = ranking.OrderByDescending(score => score).ToList();
            Debug.Log($"ランキング：{string.Join(',', ranking.Take(10))}");
            PlayerPrefs.SetString(RANKING_KEY, string.Join(',', ranking.Take(10)));
            PlayerPrefs.Save();
        }
    }
}