using System.Collections.Generic;
using System;

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
            if (words == null || words.Count <= 1)
                return 0f;

            var total = 0f;
            for (var i = 0; i < words.Count ; i++)
            {
                for(var j = i + 1 ; j < words.Count ; j++)
                {
                    total += EvaluatePair(words[i], words[j]);
                }
            }
            var pairs = (words.Count * (words.Count - 1)) / 2f;

            return total / pairs;
        }

        public float EvaluatePair(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) { return 0f; }

            var vowelA = _vowelConverter.ToVowels(a);
            var vowelB = _vowelConverter.ToVowels(b);

            if(vowelA.Length < vowelB.Length){ (vowelA,vowelB) = (vowelB,vowelA); }
            if (vowelB.Length == 0) return 0f;
            // 韻の固さの計算
            var maxMatch = 0;
            for(var i = 0 ; i <= vowelA.Length - vowelB.Length ; i++)
            {
                var tmpMatch = 0;
                for(var j = 0 ; j < vowelB.Length ; j++)
                {
                    if(vowelA[i+j] == vowelB[j]){tmpMatch++;}
                        
                }
                maxMatch = Math.Max(maxMatch,tmpMatch);
            }

            return (float)maxMatch / vowelB.Length;
        }
    }
}