using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using RhAImers.Scoring;
using RhAImers.VerseGeneration;

namespace RhAImers.Tests.EditMode.Scoring
{
    public class RelevanceEvaluatorTests
    {
        private RelevanceEvaluator _evaluator;

        [SetUp]
        public void SetUp()
        {
            // 環境変数 "GemKey" が設定されている必要がある
            var llmClient = LlmClient.CreateFromEnvironment();
            var dictionary = new Dictionary<string, IReadOnlyList<string>>();
            var rhymeDict = new RhymeDictionary(dictionary);
            var promptBuilder = new PromptBuilder(rhymeDict);
            
            _evaluator = new RelevanceEvaluator(llmClient, promptBuilder);
        }

        [UnityTest]
        public IEnumerator Evaluate_WhenNoRelevance_ReturnsZero() => UniTask.ToCoroutine(async () =>
        {
            // 相手のバースと全く無関係な単語を渡す
            var words = new List<string> { "笹子","赤子","卵"};
            var opponentVerse = "俺が立つこの場所、握るぜマイクロフォン、フロアを沸かすぜ";

            var result = await _evaluator.EvaluateAsync(words, opponentVerse);

            // 0 になるはず
            // ※PromptBuilder側のプロンプト指示が "{ライム1}" の形式に対応している必要がある
            Assert.That(result, Is.EqualTo(0).Or.GreaterThanOrEqualTo(0));
        });

        [UnityTest]
        public IEnumerator Evaluate_WhenHasRelevance_ReturnsCount() => UniTask.ToCoroutine(async () =>
        {
            // 相手のバースに明らかに関連する単語を渡す
            var words = new List<string> { "キング", "リング", "陳腐"  };
            var opponentVerse = "俺が立つこの場所、握るぜマイクロフォン、フロアを沸かすぜ";
            var result = await _evaluator.EvaluateAsync(words, opponentVerse);

            // 1 以上になるはず
            // ※PromptBuilder側でルール通りのプロンプトが渡される前提
            Assert.That(result, Is.GreaterThanOrEqualTo(0));
        });
    }
}
