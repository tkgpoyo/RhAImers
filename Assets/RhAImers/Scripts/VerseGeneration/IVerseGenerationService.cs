using System.Collections.Generic;
using RhAImers.Battle;

namespace RhAImers.VerseGeneration
{
    public interface IVerseGenerationService
    {
        Verse GenerateOpponentVerse(BattleContext context);
        Verse GeneratePlayerVerse(IReadOnlyList<string> rhymes, string opponentVerseText);
    }
}