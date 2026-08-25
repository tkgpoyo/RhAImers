using System.Collections.Generic;
using System.Linq;
using RhAImers.Scoring;
using UnityEngine;

public class RankingManager
{
    private const string RANKING_KEY = "Ranking";

    public static void UpdateRanking(IEnumerable<TurnScore> scores)
    {
        var score = scores.Sum(score => score.Total);
        var ranking = GetRanking();
        ranking.Add(score);
        ranking = ranking.OrderByDescending(score => score).ToList();
        PlayerPrefs.SetString(RANKING_KEY, string.Join(',', ranking.Take(10)));
        PlayerPrefs.Save();
    }

    public static List<int> GetRanking()
    {
        var sRanking = PlayerPrefs.GetString(RANKING_KEY);
        if (string.IsNullOrEmpty(sRanking)) { return new(); }

        return sRanking.Split(',')
                       .Where(sScore => int.TryParse(sScore, out _))
                       .Select(sScore => int.Parse(sScore)).ToList();
    }
}