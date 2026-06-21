using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RhAImers.VerseGeneration
{
    public class RhymeDictionary : IRhymeDictionary
    {
        private readonly Dictionary<string, IReadOnlyList<string>> _map;

        public RhymeDictionary(Dictionary<string, IReadOnlyList<string>> map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public IReadOnlyList<string> GetKeys()
        {
            return _map.Keys.ToList();
        }

        public IReadOnlyList<string> GetWords(string rhymeKey)
        {
            if (rhymeKey == null)
                throw new ArgumentNullException(nameof(rhymeKey));

            return _map.TryGetValue(rhymeKey, out var words)
                ? words
                : Array.Empty<string>();
        }

        public IReadOnlyList<string> GetRandomWords(string rhymeKey, int count)
        {
            var words = GetWords(rhymeKey);
            if (count <= 0 || words.Count == 0)
                return Array.Empty<string>();

            if (words.Count <= count)
                return words;

            var candidates = words.ToList();
            var selected = new List<string>(count);

            while (selected.Count < count && candidates.Count > 0)
            {
                var index = UnityEngine.Random.Range(0, candidates.Count);
                selected.Add(candidates[index]);
                candidates.RemoveAt(index);
            }

            return selected;
        }
    }
}
