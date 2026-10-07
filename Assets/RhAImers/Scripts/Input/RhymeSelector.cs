using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RhAImers.UI;
using RhAImers.VerseGeneration;
using UnityEngine;

namespace RhAImers.Input
{
    public class RhymeSelector
    {
        public List<string> SelectRandomRhyme(int rhymeNum)
        {
            var rhymeDictionary = RhymeDictionaryLoader.LoadFromResources();
            var selectedIdxs = new List<int>();
            var selectedKeys = new List<string>();
            for (int i = 0; i < rhymeNum; i++)
            {
                var selectedIdx = 0;
                do {
                    selectedIdx = Random.Range(0, rhymeDictionary.GetKeys().Count);
                } while (selectedIdxs.Contains(selectedIdx));
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