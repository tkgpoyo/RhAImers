using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RhAImers.Battle;

namespace RhAImers.VerseGeneration
{
    public class MultipleLlmVerseGenerationService : IVerseGenerationService
    {
        private readonly LlmClient _client;
        private readonly PromptBuilder _promptBuilder;

        public MultipleLlmVerseGenerationService(LlmClient client, PromptBuilder promptBuilder)
        {
            _client        = client        ?? throw new ArgumentNullException(nameof(client));
            _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        }

        public async UniTask<Verse> GenerateOpponentVerseAsync(BattleContext context, CancellationToken ct = default)
        {
            var prompt   = _promptBuilder.BuildOpponentVersePrompt(context, out var words);
            var response = await _client.Request(prompt);
            // Opponent highlights are not tracked — we don't know which words the LLM chose to rhyme.
            //return new Verse(response, Array.Empty<VerseHighlight>());
            return new Verse(response, VerseHighlightFinder.Find(response, words));
        }

        public async UniTask<Verse> GeneratePlayerVerseAsync(IReadOnlyList<string> rhymeWords, string opponentVerseText, CancellationToken ct = default)
        {
            // 1段階目：ライムの選択
            var prompt1 = _promptBuilder.BuildPlayerVersePrompt_1(rhymeWords);
            var selectedRhymesLine = await _client.Request(prompt1);
            var prompt2 = _promptBuilder.BuildPlayerVersePrompt_2(selectedRhymesLine);
            var baseVerse = await _client.Request(prompt2);
            var prompt3 = _promptBuilder.BuildPlayerVersePrompt_3(selectedRhymesLine, baseVerse);
            baseVerse = await _client.Request(prompt3);
            var prompt4 = _promptBuilder.BuildPlayerVersePrompt_4(selectedRhymesLine, baseVerse);
            var generatedVerse = await _client.Request(prompt4);

            var highlights = VerseHighlightFinder.Find(generatedVerse, rhymeWords);
            return new Verse(generatedVerse, highlights);
        }
    }
}
