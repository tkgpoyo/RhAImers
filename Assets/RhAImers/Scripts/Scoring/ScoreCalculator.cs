using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using RhAImers.Battle;
using UnityEngine;

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
                // ADD 2026/08/21 ota 平均文字数を追加
                var rhymeCount = turn.InputRhymes.Count;
                //var averageHardness = await _hardnessEvaluator.EvaluateAsync(turn.InputRhymes);
                (float averageHardness, float averageLength) = await _hardnessEvaluator.EvaluateAsync(turn.InputRhymes);
                Debug.Log(averageLength);
                var relevanceCount = await _relevanceEvaluator.EvaluateAsync(turn.InputRhymes, turn.OpponentVerse.Text);
                //scores.Add(new TurnScore(rhymeCount, averageHardness, relevanceCount));
                scores.Add(new TurnScore(rhymeCount, averageHardness, relevanceCount, averageLength));
            }

            return scores;
        }
    }
}