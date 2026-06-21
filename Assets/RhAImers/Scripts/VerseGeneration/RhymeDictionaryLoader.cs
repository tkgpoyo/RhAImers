using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RhAImers.VerseGeneration
{
    public static class RhymeDictionaryLoader
    {
        private const string DefaultResourcePath = "RhymeDictionary/rhymeDictionary";

        public static IRhymeDictionary LoadFromResources(string resourcePath = DefaultResourcePath)
        {
            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null)
            {
                throw new InvalidOperationException($"Failed to load rhyme dictionary from Resources at '{resourcePath}'.");
            }

            return LoadFromJson(textAsset.text);
        }

        public static IRhymeDictionary LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON text must not be null or empty.", nameof(json));
            }

            var wrapper = JsonUtility.FromJson<RhymeDictionaryJson>(json);
            if (wrapper?.entries == null)
            {
                throw new InvalidOperationException("Failed to parse rhyme dictionary JSON.");
            }

            var dictionary = wrapper.entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.rhymeKey) && entry.words != null)
                .ToDictionary(entry => entry.rhymeKey, entry => (IReadOnlyList<string>)entry.words.ToList());

            return new RhymeDictionary(dictionary);
        }

        [Serializable]
        private class RhymeDictionaryJson
        {
            public RhymeEntry[] entries;
        }

        [Serializable]
        private class RhymeEntry
        {
            public string rhymeKey;
            public string[] words;
        }
    }
}
