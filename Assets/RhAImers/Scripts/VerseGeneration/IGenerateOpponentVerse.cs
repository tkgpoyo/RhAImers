using RhAImers.Battle;

namespace RhAImers.VerseGeneration
{
    /// <summary>
    /// Generates a verse for the opponent (AI) based on the current battle context.
    /// The context contains all previous turns so the LLM can maintain thematic continuity.
    /// </summary>
    public interface IGenerateOpponentVerse
    {
        Verse GenerateOpponentVerse(BattleContext context);
    }
}
