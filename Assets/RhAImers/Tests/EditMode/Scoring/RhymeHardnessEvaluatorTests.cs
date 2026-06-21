using System.Collections.Generic;
using NUnit.Framework;
using RhAImers.Scoring;

namespace RhAImers.Tests.EditMode.Scoring
{
    public class RhymeHardnessEvaluatorTests
    {
        private RhymeHardnessEvaluator _evaluator;

        [SetUp]
        public void SetUp()
        {
            _evaluator = new RhymeHardnessEvaluator(new VowelConverter());
        }

        [Test]
        public void EvaluatePair_WhenSameVowels_ReturnsOne()
        {
            var score = _evaluator.EvaluatePair("aaau", "aaau");

            Assert.That(score, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void EvaluatePair_WhenOneCharacterDifferentInSameLength_ReturnsMatchRatio()
        {
            var score = _evaluator.EvaluatePair("aaau", "aqau");

            Assert.That(score, Is.EqualTo(0.75f).Within(0.001f));
        }

        [Test]
        public void EvaluatePair_WhenAllCharactersDifferent_ReturnsZero()
        {
            var score = _evaluator.EvaluatePair("aaaa", "iiii");

            Assert.That(score, Is.EqualTo(0.0f).Within(0.001f));
        }

        [Test]
        public void EvaluatePair_WhenDifferentLength_UsesBestMatchingSubsequence()
        {
            // ライム: aiu
            // オムライス: ouaiu
            // ouaiu の中に aiu が含まれているので 1.0
            var score = _evaluator.EvaluatePair("aiu", "ouaiu");

            Assert.That(score, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void EvaluatePair_WhenDifferentLengthAndPartiallyMatches_ReturnsBestMatchRatio()
        {
            // aiu と ouaia を比較する場合、
            // 長い方の部分列 aia が最も近く、a と i が一致して 2/3
            var score = _evaluator.EvaluatePair("aiu", "ouaia");

            Assert.That(score, Is.EqualTo(2.0f / 3.0f).Within(0.001f));
        }

        [Test]
        public void EvaluateAverage_WhenMultipleVowels_ReturnsAveragePairScore()
        {
            // aaau vs aqau = 0.75
            // aaau vs aqau = 0.75
            // aqau vs aqau = 1.00
            // average = 0.833...
            var vowels = new List<string>
            {
                "aaau",
                "aqau",
                "aqau"
            };

            var score = _evaluator.Evaluate(vowels);

            Assert.That(score, Is.EqualTo((0.75f + 0.75f + 1.0f) / 3.0f).Within(0.001f));
        }

        [Test]
        public void EvaluateAverage_WhenEmpty_ReturnsZero()
        {
            var score = _evaluator.Evaluate(new List<string>());

            Assert.That(score, Is.EqualTo(0.0f).Within(0.001f));
        }

        [Test]
        public void EvaluateAverage_WhenOnlyOneVowel_ReturnsZero()
        {
            var score = _evaluator.Evaluate(new List<string> { "aaau" });

            Assert.That(score, Is.EqualTo(0.0f).Within(0.001f));
        }
    }
}