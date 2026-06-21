namespace RhAImers.Scoring
{
    public class VowelConverter
    {
        public string ToVowels(string word)
        {
            // TODO: 日本語対応ができていないから実装
            if (string.IsNullOrEmpty(word)) { return string.Empty; }

            var normalized = word.ToLowerInvariant();
            var vowels = "aiueo";
            var result = string.Empty;

            foreach (var c in normalized)
            {
                if (vowels.Contains(c))
                    result += c;
            }

            return result;
        }
    }
}