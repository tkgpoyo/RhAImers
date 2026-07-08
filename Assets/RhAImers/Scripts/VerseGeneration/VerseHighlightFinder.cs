using RhAImers.Battle;
using System;
using System.Collections.Generic;
using System.Linq;

public static class VerseHighlightFinder
{
    /// <summary>
    /// Scans the generated text for every occurrence of each rhyme word and
    /// returns a highlight for each match so the UI can visually emphasise them.
    /// </summary>
    public static IReadOnlyList<VerseHighlight> Find(
        string text,
        IReadOnlyList<string> rhymes)
    {
        var highlights = new List<VerseHighlight>();
        if (string.IsNullOrEmpty(text) || rhymes == null) return highlights;

        var ranges = new List<(int Start, int End)>();
        foreach (var rhyme in rhymes)
        {
            if (string.IsNullOrEmpty(rhyme)) continue;

            var searchFrom = 0;
            while (searchFrom < text.Length)
            {
                var idx = text.IndexOf(rhyme, searchFrom, StringComparison.Ordinal);
                if (idx < 0) break;

                ranges.Add((idx, idx + rhyme.Length - 1));
                searchFrom = idx + rhyme.Length;
            }
        }

        ranges.Sort((left, right) => left.Start.CompareTo(right.Start));

        var mergedRanges = new List<(int Start, int End)>();
        foreach (var range in ranges)
        {
            if (mergedRanges.Count == 0)
            {
                mergedRanges.Add(range);
                continue;
            }

            var last = mergedRanges[^1];
            if (range.Start <= last.End + 1)
            {
                mergedRanges[^1] = (last.Start, Math.Max(last.End, range.End));
            }
            else
            {
                mergedRanges.Add(range);
            }
        }

        return mergedRanges
            .Select(range => new VerseHighlight(range.Start, range.End - range.Start + 1))
            .ToList();
    }
}