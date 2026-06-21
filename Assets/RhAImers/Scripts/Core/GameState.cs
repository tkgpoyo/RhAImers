namespace RhAImers.Core
{
    /// <summary>
    /// ゲームの状態を表す列挙型
    /// </summary>
    public enum GameState
    {
        /// <summary>タイトル画面</summary>
        Title,
        /// <summary>モード選択画面</summary>
        ModeSelect,
        /// <summary>バトル開始</summary>
        BattleStart,
        /// <summary>相手バース生成中</summary>
        OpponentVerse,
        /// <summary>ライム入力中</summary>
        RhymeInput,
        /// <summary>プレイヤーバース生成中</summary>
        VerseGeneration,
        /// <summary>ターン終了</summary>
        TurnEnd,
        /// <summary>得点計算中</summary>
        Scoring,
        /// <summary>結果表示</summary>
        Result
    }
}