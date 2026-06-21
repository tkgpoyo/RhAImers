using System.Collections.Generic;

namespace RhAImers.Battle
{
    /// <summary>バースを表すクラス</summary>
    public class Verse
    {
        /// <summary>バース文字列</summary>
        public string Text { get; }
        /// <summary>バース強調位置リスト</summary>
        public IReadOnlyList<VerseHighlight> Highlights { get; }

        public Verse(string text, IReadOnlyList<VerseHighlight> highlights)
        {
            Text = text;
            Highlights = highlights;
        }
    }

    /// <summary>バースのハイライトを表すクラス</summary>
    public class VerseHighlight
    {
        /// <summary>ハイライト開始インデックス</summary>
        public int StartIndex { get; }
        /// <summary>ハイライト部分の長さ</summary>
        public int Length { get; }

        public VerseHighlight(int startIndex, int length)
        {
            StartIndex = startIndex;
            Length = length;
        }
    }
}