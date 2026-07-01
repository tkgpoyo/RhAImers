using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

        public async UniTask<IReadOnlyList<TurnScore>> CalculateAsync(IReadOnlyList<TurnData> turns)
        {
            var scores = new List<TurnScore>();
            foreach (var turn in turns)
            {
                var rhymeCount = turn.InputRhymes.Count;
                var averageHardness = _hardnessEvaluator.Evaluate(turn.InputRhymes);
                var relevantWords = await _relevanceEvaluator.EvaluateAsync(turn.InputRhymes, turn.OpponentVerse.Text);
                scores.Add(new TurnScore(rhymeCount, averageHardness, relevantWords.Count));
            }

            return scores;
        }
    }
}