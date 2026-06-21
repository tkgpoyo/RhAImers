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
            _client = client;
            _promptBuilder = promptBuilder;
        }

        public Verse GenerateOpponentVerse(BattleContext context)
        {
            var prompt = _promptBuilder.BuildOpponentVersePrompt(context);
            var response = _client.Request(prompt);

            // TODO: ハイライト位置を正しく実装
            return new Verse(response, new VerseHighlight[0]);
        }

        public Verse GeneratePlayerVerse(IReadOnlyList<string> rhymes, string opponentVerseText)
        {
            var prompt = _promptBuilder.BuildPlayerVersePrompt(rhymes, opponentVerseText);
            var response = _client.Request(prompt);

            // TODO: ハイライト位置を正しく実装
            return new Verse(response, new VerseHighlight[0]);
        }
    }
}