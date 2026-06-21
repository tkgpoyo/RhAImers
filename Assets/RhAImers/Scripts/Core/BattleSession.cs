using System.Collections.Generic;
using RhAImers.Battle;

namespace RhAImers.Core
{
    /// <summary>
    /// バトルセッションを表すクラス
    /// </summary>
    public class BattleSession
    {
        /// <summary>ターン情報のリスト</summary>
        /// <remarks>外部公開の際は Read Only です．</remarks>
        private readonly List<TurnData> _turns = new();

        /// <summary>現在のターン番号</summary>
        public int CurrentTurnIndex => _turns.Count + 1;
        /// <summary>最大のターン番号</summary>
        public int MaxTurn { get; }
        /// <summary>ターン情報のリスト</summary>
        public IReadOnlyList<TurnData> Turns => _turns.AsReadOnly();

        public BattleSession(int maxTurn)
        {
            MaxTurn = maxTurn;
        }

        public void AddTurn(TurnData turn)
        {
            _turns.Add(turn);
        }

        public BattleContext GenerateBattleContext()
        {
            return new BattleContext(CurrentTurnIndex, new List<TurnData>(_turns));
        }

        public bool IsFinalTurn()
        {
            return _turns.Count >= MaxTurn;
        }
    }
}