using System;
using System.Collections.Generic;
using RhAImers.Battle;

namespace RhAImers.VerseGeneration
{
    /// <summary>
    /// Verse generation without LLM — uses preset templates and the player's rhyme words.
    /// Useful for offline testing or as a fallback when the API is unavailable.
    /// </summary>
    public class FixedVerseGenerationService : IVerseGenerationService
    {
        private static readonly string[] PresetOpponentVerses =
        {
            "俺のリズムは止まらない 熱い血が沸き立つ\nマイクを握る手に力 世界が変わる瞬間",
            "言葉の刃で斬り裂く 暗闇を照らす光\nビートに乗せた魂 誰も止められない炎",
            "街の角で磨いた技 今こそ見せる時が来た\nリズムと韻が織りなす 俺だけの物語を聞け",
            "挑むなら覚悟しろ ここは俺の舞台だ\n一歩も引かぬ意地 勝利を掴みに行く",
            "言葉は弾丸よりも鋭い 心を貫く詩の力\nお前には見えているか この先に続く道が",
        };

        public Verse GenerateOpponentVerse(BattleContext context)
        {
            var index = context.TurnIndex % PresetOpponentVerses.Length;
            return new Verse(PresetOpponentVerses[index], Array.Empty<VerseHighlight>());
        }

        public Verse GeneratePlayerVerse(IReadOnlyList<string> rhymes, string opponentVerseText)
        {
            var rhymeList = rhymes != null && rhymes.Count > 0
                ? string.Join("と", rhymes)
                : "言葉";

            var text = $"{rhymeList}で答える これが俺の返し\nお前のバースを超えていく 終わりなき挑戦を見せろ";

            return new Verse(text, FindRhymeHighlights(text, rhymes));
        }

        private static IReadOnlyList<VerseHighlight> FindRhymeHighlights(
            string text, IReadOnlyList<string> rhymes)
        {
            var highlights = new List<VerseHighlight>();
            if (string.IsNullOrEmpty(text) || rhymes == null) return highlights;

            foreach (var rhyme in rhymes)
            {
                if (string.IsNullOrEmpty(rhyme)) continue;

                var searchFrom = 0;
                while (searchFrom < text.Length)
                {
                    var idx = text.IndexOf(rhyme, searchFrom, StringComparison.Ordinal);
                    if (idx < 0) break;
                    highlights.Add(new VerseHighlight(idx, rhyme.Length));
                    searchFrom = idx + rhyme.Length;
                }
            }

            return highlights;
        }
    }
}
