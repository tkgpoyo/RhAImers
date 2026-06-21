using System.Collections.Generic;

namespace RhAImers.Battle
{
    /// <summary>
    /// 現在のバトル状況を表すクラス
    /// </summary>
    public class BattleContext
    {
        /// <summary>現在のターン番号</summary>
        public int TurnIndex { get; }
        /// <summary>これまでのターン情報</summary>
        public IReadOnlyList<TurnData> PreviousTurns { get; }

        public BattleContext(int turnIndex, IReadOnlyList<TurnData> previousTurns)
        {
            TurnIndex = turnIndex;
            PreviousTurns = previousTurns;
        }
    }
}