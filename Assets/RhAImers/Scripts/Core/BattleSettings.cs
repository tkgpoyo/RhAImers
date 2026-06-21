namespace RhAImers.Core
{
    /// <summary>
    /// バトル設定を表すクラス
    /// </summary>
    public class BattleSettings
    {
        /// <summary>最大ターン数</summary>
        public int MaxTurn { get; set; }
        /// <summary>入力の制限時間</summary>
        public int InputTimeLimitSec { get; set; }
        /// <summary>難易度</summary>
        public Difficulty Difficulty { get; set; }

        public BattleSettings() { }
        public BattleSettings(int maxTurn, int inputTimeLimitSec, Difficulty difficulty)
        {
            MaxTurn = maxTurn;
            InputTimeLimitSec = inputTimeLimitSec;
            Difficulty = difficulty;
        }
    }
}