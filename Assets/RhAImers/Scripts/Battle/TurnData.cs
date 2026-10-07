using System.Collections.Generic;

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
        /// <summary>韻のテーマとなる単語</summary>
        public string ThemeWord { get; }

        public TurnData(int turnIndex, Verse opponentVerse, string themeWord, IReadOnlyList<string> inputRhymes, Verse generatedPlayerVerse)
        {
            TurnIndex = turnIndex;
            OpponentVerse = opponentVerse;
            ThemeWord = themeWord;
            InputRhymes = inputRhymes;
            GeneratedPlayerVerse = generatedPlayerVerse;
        }
    }
}