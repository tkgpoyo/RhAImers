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
        public void EvaluatePair_WhenOneStringIsNullOrWhiteSpace_ReturnsZero()
        {
            Assert.That(_evaluator.EvaluatePair("aaau", null), Is.EqualTo(0.0f));
            Assert.That(_evaluator.EvaluatePair(null, "aaau"), Is.EqualTo(0.0f));
            Assert.That(_evaluator.EvaluatePair("aaau", ""), Is.EqualTo(0.0f));
            Assert.That(_evaluator.EvaluatePair("   ", "aaau"), Is.EqualTo(0.0f));
        }

        [Test]
        public void EvaluatePair_WhenBothStringsAreNullOrWhiteSpace_ReturnsZero()
        {
            Assert.That(_evaluator.EvaluatePair(null, null), Is.EqualTo(0.0f));
            Assert.That(_evaluator.EvaluatePair("", ""), Is.EqualTo(0.0f));
            Assert.That(_evaluator.EvaluatePair(" ", "   "), Is.EqualTo(0.0f));
        }

        [Test]
        public void EvaluatePair_WhenArgumentsReversed_ReturnsSameScore()
        {
            // 引数の順序を入れ替えても同じスコアになることを確認
            var score1 = _evaluator.EvaluatePair("あいう", "あいうえお"); // aiu vs aiueo -> 1.0f
            var score2 = _evaluator.EvaluatePair("あいうえお", "あいう");
            Assert.That(score1, Is.EqualTo(1.0f).Within(0.001f));
            Assert.That(score1, Is.EqualTo(score2).Within(0.001f));
        }

        [Test]
        public void EvaluatePair_WithYoonAndChouonpu_ReturnsCorrectScore()
        {
            // 「きゃー」 -> aa
            // 「わー」 -> aa
            var score = _evaluator.EvaluatePair("きゃー", "わー");
            Assert.That(score, Is.EqualTo(1.0f).Within(0.001f));
        }
        
        [Test]
        public void EvaluatePair_WhenConvertedVowelIsEmpty_ReturnsZero()
        {
            // 記号などで母音変換後に空文字になる場合
            var score = _evaluator.EvaluatePair("!!!", "???");
            Assert.That(score, Is.EqualTo(0.0f).Within(0.001f));
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
                "たたかう",
                "まったく",
                "かならず"
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