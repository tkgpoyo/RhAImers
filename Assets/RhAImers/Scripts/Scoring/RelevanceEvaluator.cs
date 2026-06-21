using System.Collections.Generic;
using RhAImers.Battle;
using RhAImers.VerseGeneration;

namespace RhAImers.Scoring
{
    public class RelevanceEvaluator
    {
        private readonly LlmClient _client;
        private readonly PromptBuilder _promptBuilder;

        public RelevanceEvaluator(LlmClient client, PromptBuilder promptBuilder)
        {
            _client = client;
            _promptBuilder = promptBuilder;
        }

        // TODO: 戻り値はintでいいかも？わからないが...要検討
        public IReadOnlyList<string> Evaluate(IReadOnlyList<string> words, string opponentVerseText)
        {
            var prompt = _promptBuilder.BuildRelevancePrompt(words, opponentVerseText);
            var response = _client.Request(prompt);
            // TODO: 頑張って関連度計算
            return new List<string>();
        }
    }
}