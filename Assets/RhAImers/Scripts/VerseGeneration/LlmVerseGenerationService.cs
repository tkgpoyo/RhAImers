using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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

        public async UniTask<Verse> GenerateOpponentVerseAsync(BattleContext context, CancellationToken ct = default)
        {
            var prompt   = _promptBuilder.BuildOpponentVersePrompt(context);
            var response = _client.Request(prompt);
            // Opponent highlights are not tracked — we don't know which words the LLM chose to rhyme.
            return new Verse(response, Array.Empty<VerseHighlight>());
        }

        public async UniTask<Verse> GeneratePlayerVerseAsync(IReadOnlyList<string> rhymeWords, string opponentVerseText, CancellationToken ct = default)
        {
            var prompt     = _promptBuilder.BuildPlayerVersePrompt(rhymeWords, opponentVerseText);
            var response   = _client.Request(prompt);
            var highlights = VerseHighlightFinder.Find(response, rhymeWords);
            return new Verse(response, highlights);
        }
    }
}
