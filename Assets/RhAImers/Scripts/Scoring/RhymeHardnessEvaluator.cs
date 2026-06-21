using System.Collections.Generic;

namespace RhAImers.Scoring
{
    public class RhymeHardnessEvaluator
    {
        private readonly VowelConverter _vowelConverter;

        public RhymeHardnessEvaluator(VowelConverter vowelConverter)
        {
            _vowelConverter = vowelConverter;
        }

        public float Evaluate(IReadOnlyList<string> words)
        {
            if (words == null || words.Count == 0)
                return 0f;

            var total = 0f;
            for (var i = 0; i < words.Count - 1; i++)
            {
                total += EvaluatePair(words[i], words[i + 1]);
            }

            return total / (words.Count - 1);
        }

        public float EvaluatePair(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) { return 0f; }

            var vowelA = _vowelConverter.ToVowels(a);
            var vowelB = _vowelConverter.ToVowels(b);

            // TODO: 多分韻の固さの計算方法が違うはず，実装必要
            return vowelA == vowelB ? 1f : 0f;
        }
    }
}