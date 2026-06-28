using System.Text;

namespace RhAImers.Scoring
{
    public class VowelConverter
    {
        public string ToVowels(string word)
        {
            if (string.IsNullOrEmpty(word)) { return string.Empty; }

            var result = new StringBuilder();
            char lastVowel = '\0';

            foreach (var c in word)
            {
                char vowel = GetVowel(c);
                if (vowel != '\0')
                {
                    if (IsSmallKana(c) && result.Length > 0)
                    {
                        // 拗音などの場合は直前の母音を上書きする
                        result[result.Length - 1] = vowel;
                    }
                    else
                    {
                        result.Append(vowel);
                    }
                    lastVowel = vowel;
                }
                else if (c == 'ー' || c == '―' || c == '-')
                {
                    if (lastVowel != '\0')
                    {
                        result.Append(lastVowel);
                    }
                }
            }

            return result.ToString();
        }

        private static char GetVowel(char c)
        {
            if ("あかさたなはまやらわがざだばぱアカサタナハマヤラワガザダバパぁゃァャゎヮヵaA".Contains(c)) return 'a';
            if ("いきしちにひみりゐぎじぢびぴイキシチニヒミリヰギジヂビピぃィiI".Contains(c)) return 'i';
            if ("うくすつぬふむゆるぐずづぶぷウクスツヌフムユルグズヅブプぅゅゥュヴuU".Contains(c)) return 'u';
            if ("えけせてねへめれゑげぜでべぺエケセテネヘメレヱゲゼデベペぇェヶeE".Contains(c)) return 'e';
            if ("おこそとのほもよろをごぞどぼぽオコソトノホモヨロヲロゴゾドボポぉょォョoO".Contains(c)) return 'o';
            if ("んンnN".Contains(c)) return 'n';
            if ("っッqQ".Contains(c)) return 'q';
            return '\0';
        }

        private static bool IsSmallKana(char c)
        {
            return "ぁぃぅぇぉゃゅょゎァィゥェォャュョヮ".Contains(c);
        }
    }
}