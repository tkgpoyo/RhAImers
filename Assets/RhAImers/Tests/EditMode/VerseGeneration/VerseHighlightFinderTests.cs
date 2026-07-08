using System.Collections.Generic;
using NUnit.Framework;
using RhAImers.Battle;

namespace RhAImers.Tests.EditMode.VerseGeneration
{
    public class VerseHighlightFinderTests
    {
        [Test]
        public void Find_WhenTextContainsRhymeMultipleTimes_ReturnsCorrectHighlightRanges()
        {
            var highlights = VerseHighlightFinder.Find("alpha beta alpha", new List<string> { "alpha" });

            Assert.That(highlights, Has.Count.EqualTo(2));
            Assert.That(highlights[0].StartIndex, Is.EqualTo(0));
            Assert.That(highlights[0].Length, Is.EqualTo(5));
            Assert.That(highlights[1].StartIndex, Is.EqualTo(11));
            Assert.That(highlights[1].Length, Is.EqualTo(5));
        }

        [Test]
        public void Find_WhenHighlightsOverlap_MergesIntoSingleRange()
        {
            var highlights = VerseHighlightFinder.Find("abcde fghij", new List<string> { "cde", "fghi" });

            Assert.That(highlights, Has.Count.EqualTo(1));
            Assert.That(highlights[0].StartIndex, Is.EqualTo(2));
            Assert.That(highlights[0].Length, Is.EqualTo(8));
        }
    }
}
