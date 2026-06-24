using System;
using System.Collections.Generic;
using RhAImers.Battle;

namespace RhAImers.VerseGeneration
{
    public class LlmVerseGenerationService : IVerseGenerationService
    {
        private readonly LlmClient _client;
        private readonly PromptBuilder _promptBuilder;

        public LlmVerseGenerationService(LlmClient client, PromptBuilder promptBuilder)
        {
            _client        = client        ?? throw new ArgumentNullException(nameof(client));
            _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        }

        public Verse GenerateOpponentVerse(BattleContext context)
        {
            var prompt   = _promptBuilder.BuildOpponentVersePrompt(context);
            var response = _client.Request(prompt);
            // Opponent highlights are not tracked — we don't know which words the LLM chose to rhyme.
            return new Verse(response, Array.Empty<VerseHighlight>());
        }

        public Verse GeneratePlayerVerse(IReadOnlyList<string> rhymes, string opponentVerseText)
        {
            var prompt     = _promptBuilder.BuildPlayerVersePrompt(rhymes, opponentVerseText);
            var response   = _client.Request(prompt);
            var highlights = FindRhymeHighlights(response, rhymes);
            return new Verse(response, highlights);
        }

        /// <summary>
        /// Scans the generated text for every occurrence of each rhyme word and
        /// returns a highlight for each match so the UI can visually emphasise them.
        /// </summary>
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
