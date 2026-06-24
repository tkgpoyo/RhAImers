using System.Collections.Generic;
using RhAImers.Battle;

namespace RhAImers.VerseGeneration
{
    /// <summary>
    /// Generates a verse for the player that:
    ///   - responds to the opponent's verse, and
    ///   - rhymes with the words the player has provided.
    /// </summary>
    public interface IGeneratePlayerVerse
    {
        /// <param name="rhymeWords">Words the generated verse must rhyme with as much as possible.</param>
        /// <param name="opponentVerseText">The opponent's verse that the player verse must answer.</param>
        Verse GeneratePlayerVerse(IReadOnlyList<string> rhymeWords, string opponentVerseText);
    }
}
