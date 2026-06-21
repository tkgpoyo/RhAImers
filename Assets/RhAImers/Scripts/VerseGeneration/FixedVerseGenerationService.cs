using System;
using System.Collections.Generic;
using RhAImers.Battle;

namespace RhAImers.VerseGeneration
{
    /// <summary>
    /// Generate Fixed Verse.  That is, generate verse without using LLM.
    /// </summary>
    public class FixedVerseGenerationService : IVerseGenerationService
    {
        // TODO: Implement both verse generation functions without LLM. Any verses are OK, but please generate player verse using inputted rhymes.
        public Verse GenerateOpponentVerse(BattleContext context)
        {
            throw new NotImplementedException();
        }

        public Verse GeneratePlayerVerse(IReadOnlyList<string> rhymes, string opponentVerseText)
        {
            throw new NotImplementedException();
        }
    }
}