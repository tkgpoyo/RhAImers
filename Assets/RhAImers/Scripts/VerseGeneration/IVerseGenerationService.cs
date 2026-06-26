using Cysharp.Threading.Tasks;
using RhAImers.Battle;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RhAImers.VerseGeneration
{
    /// <summary>
    /// Convenience composite interface that covers both verse-generation roles.
    /// Implement this when a single service handles both opponent and player verse generation.
    /// </summary>
    public interface IVerseGenerationService
    {
        /// <summary>
        /// Generates the opponent's next rap verse based on the full battle
        /// history seen so far.
        /// </summary>
        /// <param name="context">
        ///   A chronological transcript of every verse already delivered in
        ///   this battle (opponent and player alike). Pass <c>null</c> or
        ///   empty string to open the battle fresh.
        /// </param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>The opponent's new verse as a plain string.</returns>
        UniTask<Verse> GenerateOpponentVerseAsync(BattleContext context, CancellationToken ct = default);
 
        /// <summary>
        /// Generates the player's rebuttal verse.
        /// The verse must answer the opponent's latest bars and incorporate
        /// the supplied rhyme words as densely as possible.
        /// </summary>
        /// <param name="rhymeWords">
        ///   Words the player locked in for this round — the verse must
        ///   rhyme with all of them (end-rhyme or mid-line).
        /// </param>
        /// <param name="opponentVerse">
        ///   The verse returned by <see cref="GenerateOpponentVerseAsync"/>
        ///   that the player is responding to.
        /// </param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>The player's rebuttal verse as a plain string.</returns>
        UniTask<Verse> GeneratePlayerVerseAsync(IReadOnlyList<string> rhymeWords, string opponentVerseText, CancellationToken ct = default);
    }
}