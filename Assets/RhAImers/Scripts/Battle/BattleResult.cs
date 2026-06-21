using System.Collections.Generic;
using System.Linq;
using RhAImers.Scoring;

namespace RhAImers.Battle
{
    /// <summary>
    /// バトル結果を表すクラス
    /// </summary>
    public class BattleResult
    {
        /// <summary>全てのターンの情報</summary>
        public IReadOnlyList<TurnData> Turns { get; }
        /// <summary>全てのターンの得点</summary>
        public IReadOnlyList<TurnScore> TurnScores { get; }
        /// <summary>合計点数</summary>
        public int TotalScore { get; }

        public BattleResult(IReadOnlyList<TurnData> turns, IReadOnlyList<TurnScore> turnScores)
        {
            Turns = turns;
            TurnScores = turnScores;
            
            TotalScore = turnScores.Sum(turnScore => turnScore.Total);      // 合計点数は各ターンの点数の合計
        }
    }
}