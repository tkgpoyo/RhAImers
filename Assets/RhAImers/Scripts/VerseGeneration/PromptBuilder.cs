using System;
using System.Collections.Generic;
using System.Linq;
using RhAImers.Battle;
using UnityEngine;

namespace RhAImers.VerseGeneration
{
    public class PromptBuilder
    {
        private readonly IRhymeDictionary _rhymeDictionary;

        public PromptBuilder(IRhymeDictionary rhymeDictionary)
        {
            _rhymeDictionary = rhymeDictionary ?? throw new ArgumentNullException(nameof(rhymeDictionary));
        }

        /// <summary>
        /// Builds a prompt that makes the LLM generate an opponent (AI) verse.
        /// The verse should challenge the player and use the supplied rhyme words.
        /// </summary>
        public string BuildOpponentVersePrompt(BattleContext context)
        {
            var rhymeKey = SelectRhymeKey();
            var words    = _rhymeDictionary.GetRandomWords(rhymeKey, 4);
            var wordList = words.Any() ? string.Join("・", words) : "(韻語なし)";

            return
$@"あなたはラップバトルの対戦相手AIです。プレイヤーに挑む短いバース（2〜4行）を日本語で作成してください。

条件:
- 以下の韻語を積極的に使用すること: {wordList}
- プレイヤーへの挑発・挑戦を込めた内容にすること
- 1行あたり14〜20音程度のリズムを意識すること
- ターン: {context.TurnIndex + 1}
{DescribePreviousTurns(context.PreviousTurns)}

バースの本文のみを出力してください。説明・注釈は不要です。";
        }

        /// <summary>
        /// Builds a prompt that makes the LLM generate a player verse.
        /// The verse must answer the opponent and rhyme with the player's chosen words.
        /// </summary>
        public string BuildPlayerVersePrompt(IReadOnlyList<string> rhymes, string opponentVerseText)
        {
            var rhymeHint = rhymes != null && rhymes.Count > 0
                ? string.Join("・", rhymes)
                : "(韻語の指定なし)";

            return
$@"あなたはラップバトルのAIアシスタントです。プレイヤーの代わりに返しのバース（2〜4行）を日本語で作成してください。

【相手のバース】
{opponentVerseText}

【使用する韻語（必須）】
{rhymeHint}

条件:
- 上記の韻語をできる限り多く使用し、韻を踏むことを最優先すること
- 相手のバースへの反撃・返答になる内容にすること
- 1行あたり14〜20音程度のリズムを意識すること

バースの本文のみを出力してください。説明・注釈は不要です。";
        }

        /// <summary>
        /// Builds a prompt that asks the LLM to pick which rhyme word fits best
        /// with the opponent's verse (used for automated word selection).
        /// </summary>
        public string BuildRelevancePrompt(IReadOnlyList<string> rhymes, string opponentVerseText)
        {
            var rhymeHint = rhymes != null && rhymes.Count > 0
                ? string.Join("・", rhymes)
                : "(候補なし)";

            return
$@"以下の韻語候補の中から、相手のバースに最もよく呼応するものを一つだけ選んでください。

【韻語候補】
{rhymeHint}

【相手のバース】
{opponentVerseText}

選ぶ基準: 韻の響きの近さと、バースとの意味的なつながりの強さ。
選んだ韻語のみを出力してください。";
        }

        private string SelectRhymeKey()
        {
            var keys = _rhymeDictionary.GetKeys();
            if (keys == null || keys.Count == 0) return string.Empty;
            return keys[Random.Range(0, keys.Count)];
        }

        private static string DescribePreviousTurns(IReadOnlyList<TurnData> previousTurns)
        {
            if (previousTurns == null || previousTurns.Count == 0)
                return "前のターン: なし";

            var lines = previousTurns.Select((turn, i) =>
                $"ターン{i + 1}: 相手「{turn.OpponentVerse.Text}」 / プレイヤー韻語「{string.Join("・", turn.InputRhymes)}」");

            return "前のターン:\n" + string.Join("\n", lines);
        }
    }
}
