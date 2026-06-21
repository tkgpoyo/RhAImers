using System.Collections.Generic;

namespace RhAImers.VerseGeneration
{
    public interface IRhymeDictionary
    {
        IReadOnlyList<string> GetKeys();
        IReadOnlyList<string> GetWords(string rhymeKey);
        IReadOnlyList<string> GetRandomWords(string rhymeKey, int count);
    }
}
