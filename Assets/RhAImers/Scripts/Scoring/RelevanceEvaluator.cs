using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

        public async UniTask<int> EvaluateAsync(IReadOnlyList<string> words, string opponentVerseText)
        {
            var prompt = _promptBuilder.BuildRelevancePrompt(words, opponentVerseText);
            var response = await _client.Request(prompt);

            if (string.IsNullOrWhiteSpace(response) || response.ToLower().Contains("null"))
            {
                return 0;
            }

            var match = System.Text.RegularExpressions.Regex.Match(response, @"\{([^}]*)\}");
            if (match.Success)
            {
                var content = match.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(content)) return 0;
                
                var items = content.Split(',');
                return items.Length;
            }

            return 0;
        }
    }
}