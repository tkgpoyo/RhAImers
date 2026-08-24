using System.Collections.Generic;
using System;
using Cysharp.Threading.Tasks;
using RhAImers.VerseGeneration;
using System.Linq;

namespace RhAImers.Scoring
{
    public class RhymeHardnessEvaluator
    {
        private readonly VowelConverter _vowelConverter;
        private readonly LlmClient _client;
        private readonly PromptBuilder _promptBuilder;

        public RhymeHardnessEvaluator(VowelConverter vowelConverter, LlmClient client = null, PromptBuilder promptBuilder = null)
        {
            _vowelConverter = vowelConverter;
            _client = client;
            _promptBuilder = promptBuilder;
        }

        // CHG 2026/08/21 ota 戻り値を float から Tuple<float, float> に変更（平均文字数の取得）
        public async UniTask<(float averageHardness, float averageLength)> EvaluateAsync(IReadOnlyList<string> originalWords)
        {
            if (originalWords == null || originalWords.Count <= 1)
                return new(0f, 0f);

            IReadOnlyList<string> words = originalWords;
            string[] converted = originalWords.ToArray();

            // LLMを使ってふりがなに変換
            if (_client != null && _promptBuilder != null)
            {
                try
                {
                    var prompt = _promptBuilder.BuildPhoneticConversionPrompt(originalWords);
                    var response = await _client.Request(prompt);
                    if (!string.IsNullOrWhiteSpace(response))
                    {
                        converted = response.Split(',');
                        if (converted.Length == originalWords.Count)
                        {
                            var trimmed = new List<string>();
                            foreach (var w in converted)
                            {
                                trimmed.Add(w.Trim());
                            }
                            words = trimmed;
                            UnityEngine.Debug.Log($"ふりがな変換結果: {string.Join(", ", words)}");
                        }
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"ふりがな変換中にエラーが発生しました: {e.Message}");
                }
            }

            return new(Evaluate(words), (float)converted.Average(w => w.Length));
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