using UnityEngine;

namespace RhAImers.Scoring
{
    public class TurnScore
    {
        public int RhymeCount { get; }
        public float AverageHardness { get; }
        public int RelevanceCount { get; }
        // ADD 2026/08/21 ota ライムの平均長さを追加
        public float AverageLength { get; }
        /// <summary>合計点数</summary>
        /// TODO: スコア計算暫定
        //public int Total => Mathf.FloorToInt(RhymeCount * AverageHardness * (RelevanceCount+1));
        public int Total => Mathf.FloorToInt(RhymeCount * AverageHardness * AverageLength);

        // ADD 2026/08/21 ota ライムの平均長さを追加
        public TurnScore(int rhymeCount, float averageHardness, int relevanceCount, float averageLength)
        {
            RhymeCount = rhymeCount;
            AverageHardness = averageHardness;
            RelevanceCount = relevanceCount;
            AverageLength = averageLength;
        }
    }
}