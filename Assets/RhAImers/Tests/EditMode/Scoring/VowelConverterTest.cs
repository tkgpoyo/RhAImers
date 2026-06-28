using NUnit.Framework;
using RhAImers.Scoring;

namespace RhAImers.Tests.EditMode
{
    public class VowelConverterTest
    {
        [Test]
        public void ToVowels_NormalKana_ReturnsVowels()
        {
            var converter = new VowelConverter();
            Assert.AreEqual("aiueo", converter.ToVowels("あいうえお"));
            Assert.AreEqual("aiueo", converter.ToVowels("アイウエオ"));
            Assert.AreEqual("aiueo", converter.ToVowels("かきくけこ"));
        }

        [Test]
        public void ToVowels_HatsuonAndSokuon_ReturnsNAndQ()
        {
            var converter = new VowelConverter();
            Assert.AreEqual("anq", converter.ToVowels("あんっ"));
        }

        [Test]
        public void ToVowels_Yoon_OverwritesPreviousVowel()
        {
            var converter = new VowelConverter();
            Assert.AreEqual("a", converter.ToVowels("きゃ"));
            Assert.AreEqual("u", converter.ToVowels("しゅ"));
            Assert.AreEqual("o", converter.ToVowels("ちょ"));
            Assert.AreEqual("a", converter.ToVowels("ふぁ"));
        }

        [Test]
        public void ToVowels_Choonpu_CopiesPreviousVowel()
        {
            var converter = new VowelConverter();
            Assert.AreEqual("aa", converter.ToVowels("あー"));
            Assert.AreEqual("uua", converter.ToVowels("しゅーや"));
        }

        [Test]
        public void ToVowels_ComplexWord_ReturnsCorrectVowels()
        {
            var converter = new VowelConverter();
            // RhAImers -> ライマーズ -> r a i m a a z u -> a i a a u
            Assert.AreEqual("aiaau", converter.ToVowels("ライマーズ"));
        }
    }
}
