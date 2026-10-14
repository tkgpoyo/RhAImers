using System.Collections.Generic;
using RhAImers.Input;

namespace RhAImers.Battle
{
    /// <summary>
    /// ターン情報を表すクラス
    /// </summary>
    public class TurnData
    {
        /// <summary>ターン番号</summary>
        public int TurnIndex { get; }
        /// <summary>相手のバース</summary>
        public Verse OpponentVerse { get; }
        /// <summary>入力されたライムのリスト</summary>
        public IReadOnlyList<string> InputRhymes { get; }
        /// <summary>プレイヤーのバース</summary>
        public Verse GeneratedPlayerVerse { get; }
        /// <summary>お題語</summary>
        public ThemeRhyme Theme { get; }

        public TurnData(int turnIndex, Verse opponentVerse, ThemeRhyme theme, IReadOnlyList<string> inputRhymes, Verse generatedPlayerVerse)
        {
            TurnIndex = turnIndex;
            OpponentVerse = opponentVerse;
            Theme = theme;
            InputRhymes = inputRhymes;
            GeneratedPlayerVerse = generatedPlayerVerse;
        }
    }
}