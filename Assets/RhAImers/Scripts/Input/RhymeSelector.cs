using System.Collections.Generic;
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
        public List<string> SelectRandomRhyme(int rhymeNum, Difficulty difficulty)
        {
            var rhymeDictionary = RhymeDictionaryLoader.LoadFromResources();
            var selectedIdxs = new List<int>();
            var selectedKeys = new List<string>();
            var invalidIdxs = new List<int>();
            for (int i = 0; i < rhymeNum; i++)
            {
                var selectedIdx = 0;
                do {
                    selectedIdx = Random.Range(0, rhymeDictionary.GetKeys().Count);
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

                selectedKeys.Add(rhymeDictionary.GetKeys()[selectedIdx]);
            }
            var selectedRhymes = new List<string>();
            foreach (var key in selectedKeys)
            {
                selectedRhymes.Add(rhymeDictionary.GetRandomWords(key, 1)[0]);
            }
            return selectedRhymes;
        }
    }
}