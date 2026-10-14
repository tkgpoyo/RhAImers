using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using RhAImers.Core;
using RhAImers.UI;
using RhAImers.VerseGeneration;
using UnityEngine;

namespace RhAImers.Input
{
    public class RhymeSelector
    {
        public List<ThemeRhyme> SelectRandomRhyme(int rhymeNum, Difficulty difficulty)
        {
            var rhymeDictionary = RhymeDictionaryLoader.LoadFromResources();
            var selectedIdxs = new List<int>();
            var selectedKeys = new List<string>();
            var invalidIdxs = new List<int>();
            var keyCount = rhymeDictionary.GetKeys().Count;
            // 候補数に達するか，全キーを調べ終わるまで抽選する（条件に合うキーが足りなくても無限ループしない）
            while (selectedKeys.Count < rhymeNum && selectedIdxs.Count + invalidIdxs.Count < keyCount)
            {
                var selectedIdx = 0;
                do {
                    selectedIdx = Random.Range(0, keyCount);
                } while (selectedIdxs.Contains(selectedIdx) || invalidIdxs.Contains(selectedIdx));

                var valid = true;
                var themeLength = rhymeDictionary.GetKeys()[selectedIdx].Length;
                switch (difficulty)
                {
                    case Difficulty.Easy:
                        if (themeLength != 3)
                        {
                            valid = false;
                        }
                        break;
                    case Difficulty.Normal:
                        if (themeLength != 4)
                        {
                            valid = false;
                        }
                        break;
                    case Difficulty.Hard:
                        if (themeLength < 5)
                        {
                            valid = false;
                        }
                        break;
                }
                if (!valid)
                {
                    invalidIdxs.Add(selectedIdx);
                    continue;
                }

                selectedIdxs.Add(selectedIdx);
                selectedKeys.Add(rhymeDictionary.GetKeys()[selectedIdx]);
            }
            var selectedRhymes = new List<string>();
            foreach (var key in selectedKeys)
            {
                selectedRhymes.Add(rhymeDictionary.GetRandomWords(key, 1)[0]);
            }
            return selectedKeys.Zip(selectedRhymes, (key, rhyme) => new ThemeRhyme(rhyme, key)).ToList();
        }
    }

    public class ThemeRhyme
    {
        public string Word { get; }
        public string Vowel { get; }

        public ThemeRhyme(string word, string vowel)
        {
            Word = word;
            Vowel = vowel;
        }
    }
}