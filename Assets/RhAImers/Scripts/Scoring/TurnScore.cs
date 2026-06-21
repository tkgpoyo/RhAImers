using UnityEngine;

namespace RhAImers.Scoring
{
    public class TurnScore
    {
        public int RhymeCount { get; }
        public float AverageHardness { get; }
        public int RelevanceCount { get; }
        /// <summary>合計点数</summary>
        /// TODO: 合計点数適当に書いたが，これでいいの？要検討
        public int Total => Mathf.FloorToInt(RhymeCount * AverageHardness * RelevanceCount);

        public TurnScore(int rhymeCount, float averageHardness, int relevanceCount)
        {
            RhymeCount = rhymeCount;
            AverageHardness = averageHardness;
            RelevanceCount = relevanceCount;
        }
    }
}