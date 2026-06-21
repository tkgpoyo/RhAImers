using System.Collections.Generic;
using RhAImers.Battle;

namespace RhAImers.Scoring
{
    public class ScoreCalculator
    {
        private readonly RhymeHardnessEvaluator _hardnessEvaluator;
        private readonly RelevanceEvaluator _relevanceEvaluator;

        public ScoreCalculator(RhymeHardnessEvaluator hardnessEvaluator, RelevanceEvaluator relevanceEvaluator)
        {
            _hardnessEvaluator = hardnessEvaluator;
            _relevanceEvaluator = relevanceEvaluator;
        }

        public IReadOnlyList<TurnScore> Calculate(IReadOnlyList<TurnData> turns)
        {
            var scores = new List<TurnScore>();
            foreach (var turn in turns)
            {
                var rhymeCount = turn.InputRhymes.Count;
                var averageHardness = _hardnessEvaluator.Evaluate(turn.InputRhymes);
                var relevanceCount = _relevanceEvaluator.Evaluate(turn.InputRhymes, turn.OpponentVerse.Text).Count;
                scores.Add(new TurnScore(rhymeCount, averageHardness, relevanceCount));
            }

            return scores;
        }
    }
}