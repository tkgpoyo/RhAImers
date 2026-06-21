using System;
using System.Collections.Generic;
using System.Linq;
using RhAImers.Battle;
using UnityEngine;

namespace RhAImers.VerseGeneration
{
    // TODO: 全てのプロンプトはAIが勝手に作成したものだから，多分良くない．いい感じにプロンプトを変えてください
    public class PromptBuilder
    {
        private readonly IRhymeDictionary _rhymeDictionary;

        public PromptBuilder(IRhymeDictionary rhymeDictionary)
        {
            _rhymeDictionary = rhymeDictionary ?? throw new ArgumentNullException(nameof(rhymeDictionary));
        }

        public string BuildOpponentVersePrompt(BattleContext context)
        {
            var rhymeKey = SelectRhymeKey(context);
            var words = _rhymeDictionary.GetRandomWords(rhymeKey, 4);
            var wordList = words.Any() ? string.Join("、", words) : "(辞書から韻語を取得できませんでした)";

            return $@"あなたはラッパーです。
以下の条件に従って、韻を踏んだ日本語のバースを作成してください。

・韻の候補: {wordList}
・現在のターン: {context.TurnIndex}
{DescribePreviousTurns(context.PreviousTurns)}

出力は本文のみとし、余計な説明は書かないでください。";
        }

        public string BuildPlayerVersePrompt(IReadOnlyList<string> rhymes, string opponentVerseText)
        {
            var rhymeHint = rhymes != null && rhymes.Count > 0
                ? string.Join("、", rhymes)
                : "(入力された韻語がありません)";

            return $@"あなたはラッパーです。
以下の条件に従って、相手のバースに続くプレイヤーのバースを生成してください。

・相手のバース:
{opponentVerseText}
・使用する韻語: {rhymeHint}

出力は本文のみとし、余計な説明は含めないでください。";
        }

        public string BuildRelevancePrompt(IReadOnlyList<string> rhymes, string opponentVerseText)
        {
            var rhymeHint = rhymes != null && rhymes.Count > 0
                ? string.Join("、", rhymes)
                : "(候補がありません)";

            return $@"以下の韻語候補の中から、相手のバースにもっとも関連性が高いものを選んでください。

候補: {rhymeHint}
相手のバース:
{opponentVerseText}

選択は韻の響きと意味のつながりを重視してください。";
        }

        private string SelectRhymeKey(BattleContext context)
        {
            var keys = _rhymeDictionary.GetKeys();
            if (keys == null || keys.Count == 0)
            {
                return string.Empty;
            }

            var index = UnityEngine.Random.Range(0, keys.Count);
            return keys[index];
        }

        private string DescribePreviousTurns(IReadOnlyList<TurnData> previousTurns)
        {
            if (previousTurns == null || previousTurns.Count == 0)
            {
                return "前のターンはありません。";
            }

            var descriptions = previousTurns.Select((turn, index) =>
                $"ターン{index + 1}: {turn.OpponentVerse.Text} / プレイヤー: {string.Join("、", turn.InputRhymes)}");
            return string.Join("\n", descriptions);
        }
    }
}
