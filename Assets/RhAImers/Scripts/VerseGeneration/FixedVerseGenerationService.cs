using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RhAImers.Battle;
using UnityEditor;

namespace RhAImers.VerseGeneration
{
    /// <summary>
    /// Verse generation without LLM — uses preset templates and the player's rhyme words.
    /// Useful for offline testing or as a fallback when the API is unavailable.
    /// </summary>
    public class FixedVerseGenerationService : IVerseGenerationService
    {
        private static readonly string[] PresetOpponentVerses =
        {
            "今日勝つために立ち上がる\n今日勝つために神がかる\n最後の最後は愛が勝つ\n覚えとけこれがライマーズ\n",
            "ここじゃとうに死んだ太陽\n今は当たったスポットライト\n一度戦ったなら最後\nケリをつけて決めるぞ最強\n",
            "全力で来るのは好都合\n尽きるまで突っ切る勝負論\nゴールに向かいロックオン\n駆け抜けるぜトップロード\n",
            "お待たせラップAIの出番だ\nお前はなれて客寄せパンダ\nしっかり返すぜ今アンサー\n喉で弾くロケットランチャー\n",
            "マイク持って見せる行動力\n奏でる即興の協奏曲\n湧かせてやる五臓六腑\n音を乗りこなす暴走族\n",
        };

        private static readonly IReadOnlyList<string>[] PresetRhymeWords = {
             new List<string>() { "立ち上がる", "神がかる", "愛が勝つ", "ライマーズ" },
             new List<string>() { "太陽", "ライト", "最後", "最強" },
             new List<string>() { "好都合", "勝負論", "ロックオン", "トップロード" },
             new List<string>() { "出番だ", "パンダ", "アンサー", "ランチャー" },
             new List<string>() { "行動力", "協奏曲", "五臓六腑", "暴走族" },
        };

        public UniTask<Verse> GenerateOpponentVerseAsync(BattleContext context, CancellationToken token = default)
        {
            var index = context.TurnIndex % PresetOpponentVerses.Length;
            return UniTask.FromResult(new Verse(PresetOpponentVerses[index], VerseHighlightFinder.Find(PresetOpponentVerses[index], PresetRhymeWords[index])));
        }

        public UniTask<Verse> GeneratePlayerVerseAsync(IReadOnlyList<string> rhymes, string opponentVerseText, CancellationToken token = default)
        {
            var rhymeList = rhymes != null && rhymes.Count > 0
                ? string.Join("と", rhymes)
                : "言葉";

            var text = $"{rhymeList}で答える これが俺の返し\nお前のバースを超えていく 終わりなき挑戦を見せろ";

            return UniTask.FromResult(new Verse(text, VerseHighlightFinder.Find(text, rhymes)));
        }
    }
}
