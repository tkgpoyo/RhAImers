using RhAImers.Battle;
using System;
using System.Collections.Generic;

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

        var rhymeSet = new HashSet<string>(rhymes, StringComparer.OrdinalIgnoreCase);
        foreach (var rhyme in rhymeSet) {
            if (string.IsNullOrEmpty(rhyme)) continue;

            var searchFrom = 0;
            while (searchFrom < text.Length) {
                var idx = text.IndexOf(rhyme, searchFrom, StringComparison.Ordinal);
                if (idx < 0) break;

                highlights.Add(new VerseHighlight(idx, rhyme.Length));
                searchFrom = idx + rhyme.Length;
            }
        }

        return highlights;
    }
}