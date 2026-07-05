using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public string BuildOpponentVersePrompt(BattleContext context, out IReadOnlyList<string> words)
        {
            var rhymeKey = SelectRhymeKey();
            words    = _rhymeDictionary.GetRandomWords(rhymeKey, 4);
            var wordList = words.Any() ? string.Join("・", words) : "(韻語なし)";
            Debug.Log($"rhymeKey: {rhymeKey}, words: {words}");

            var sb = new StringBuilder();
            sb.Append(
@"あなたはラップバトルの対戦相手AIです。プレイヤーに挑む短いバース4行を日本語で作成してください。

条件:

-以下の韻語を各行の最後で使用すること: ")
              .AppendLine(wordList)
              .Append(
@" - プレイヤーへの挑発・挑戦を込めた内容にすること
- 自分の強さを誇示する内容にすること
- 韻語は単独で用いずに何かしらの単語を付けること
- 韻語に付加した単語と韻語の間には助詞を入れてください
- 1行あたり文節数は2個にすること
- 各文節の構成は[名詞][助詞][動詞]か[名詞][助詞][名詞]とする
- 助詞は「は」「が」「の」「を」「に」「と」「へ」「で」のうち，一種類だけを用いないこと
- 各行の単語は被らないようにすること
- 相手を攻撃するか自分を誇示するかは各行の各文節で統一すること
- ターン: { context.TurnIndex + 1}
{ DescribePreviousTurns(context.PreviousTurns)}

バースの本文のみを出力してください。説明・注釈は不要です。");
            return sb.ToString();
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

            var sb = new StringBuilder();
            sb.Append(
@"あなたはラップバトルのAIアシスタントです。プレイヤーの代わりに返しのバース4行を日本語で作成してください

【相手のバース】
")
                .AppendLine(opponentVerseText)
                .AppendLine(
@"

【使用する韻語（必須）】")
                .AppendLine(rhymeHint)
                .Append(
@"

        条件:

            -韻語を各行の最後で使用すること
            - 相手への挑発・挑戦を込めた内容にすること
            - 自分の強さを誇示する内容にすること
            - 韻語は単独で用いずに何かしらの単語を付けること
            - 韻語に付加した単語と韻語の間には助詞を入れてください
            - 各文節の構成は[名詞][助詞][動詞]か[名詞][助詞][名詞]とする
            - 助詞は「は」「が」「の」「を」「に」「と」「へ」「で」のうち，一種類だけを用いないこと
            - 各行の表現や単語, 用言の活用形は被らないようにすること
            - 相手を攻撃するか自分を誇示するかは各行の各文節で統一すること
            - 1行あたり文節数は2個程度にすること

            バースの本文のみを出力してください。説明・注釈は不要です。");
            return sb.ToString();
        }

        public string BuildRelevancePrompt(IReadOnlyList<string> rhymes, string opponentVerseText)
        {
            var rhymeHint = rhymes != null && rhymes.Count > 0
                ? string.Join("・", rhymes)
                : "(韻語の指定なし)";

            var sb = new StringBuilder();
            sb.AppendLine(
@"以下の韻語から，相手のバースに関連するもののみを抽出しなさい．

【韻語候補】
")
                .AppendLine(rhymeHint)
                .AppendLine(
@"【相手のバース】")
                .AppendLine(opponentVerseText)
                .AppendLine(
@" 判定は以下の順序で行うこと． 

1.相手のバースに関連する語彙がない場合は「null」と出力して終える

2.相手のバースに関連する語彙が有る場合はそれを{{ ライム1, ライム2}}
            のように包んで出力する
");

            return sb.ToString();
        }

        private string SelectRhymeKey()
        {
            var keys = _rhymeDictionary.GetKeys();
            if (keys == null || keys.Count == 0) return string.Empty;
            return keys[UnityEngine.Random.Range(0, keys.Count)];
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
