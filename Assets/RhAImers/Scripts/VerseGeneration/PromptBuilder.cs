using RhAImers.Battle;
using System.Collections.Generic;

namespace RhAImers.VerseGeneration
{
    public class PromptBuilder
    {
        // TODO: プロンプト作成処理の実装(RhymeDictionaryを使いそう？)
        public string BuildOpponentVersePrompt(BattleContext context)
        {
            return string.Empty;
        }

        public string BuildPlayerVersePrompt(IReadOnlyList<string> rhymes, string opponentVerseText)
        {
            return string.Empty;
        }

        public string BuildRelevancePrompt(IReadOnlyList<string> rhymes, string opponentVerseText)
        {
            return string.Empty;
        }
    }
}