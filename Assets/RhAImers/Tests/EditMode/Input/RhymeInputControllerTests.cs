using System.Reflection;
using NUnit.Framework;
using RhAImers.Input;
using UnityEngine;

namespace RhAImers.Tests.EditMode.Input
{
    public class RhymeInputControllerTests
    {
        private static System.Reflection.MethodInfo GetIsNonAlphabeticLettersOnlyMethod()
        {
            return typeof(RhymeInputController).GetMethod("IsNonAlphabeticLettersOnly", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [Test]
        public void IsNonAlphabeticLettersOnly_WhenInputContainsOnlyAsciiLetters_ReturnsFalse()
        {
            var method = GetIsNonAlphabeticLettersOnlyMethod();

            var result = (bool)method.Invoke(null, new object[] { "abcXYZ" });

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsNonAlphabeticLettersOnly_WhenInputContainsPunctuationOnly_ReturnsFalse()
        {
            var method = GetIsNonAlphabeticLettersOnlyMethod();

            var result = (bool)method.Invoke(null, new object[] { "[\"!" });

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsNonAlphabeticLettersOnly_WhenInputContainsFullwidthPunctuation_ReturnsFalse()
        {
            var method = GetIsNonAlphabeticLettersOnlyMethod();

            var result = (bool)method.Invoke(null, new object[] { "・．。" });

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsNonAlphabeticLettersOnly_WhenInputContainsEmoji_ReturnsFalse()
        {
            var method = GetIsNonAlphabeticLettersOnlyMethod();

            var result = (bool)method.Invoke(null, new object[] { "あ🐱" });

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsNonAlphabeticLettersOnly_WhenInputContainsDigits_ReturnsFalse()
        {
            var method = GetIsNonAlphabeticLettersOnlyMethod();

            var result = (bool)method.Invoke(null, new object[] { "12345" });

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsNonAlphabeticLettersOnly_WhenInputContainsJapaneseLettersOnly_ReturnsTrue()
        {
            var method = GetIsNonAlphabeticLettersOnlyMethod();

            var result = (bool)method.Invoke(null, new object[] { "あいうえおアイウエオ東京" });

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsNonAlphabeticLettersOnly_WhenInputContainsNormalizationEquivalentHiragana_ReturnsTrue()
        {
            var method = GetIsNonAlphabeticLettersOnlyMethod();

            // 「が」の結合文字表現を含む文字列
            var result = (bool)method.Invoke(null, new object[] { "がぎ" });

            Assert.That(result, Is.True);
        }
    }
}
