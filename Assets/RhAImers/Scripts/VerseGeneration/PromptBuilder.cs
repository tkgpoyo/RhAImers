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

        public string BuildPhoneticConversionPrompt(IReadOnlyList<string> words)
        {
            var sb = new StringBuilder();
            sb.Append(
@"以下の単語群について、それぞれの読み（ひらがなまたはカタカナ）を以下のルールに従って決定してください。

ルール:
- 漢字は適切な読みを選択してください。
複数の読みが存在する場合は、入力された単語群全体で「母音の並び（韻）」が最も一致するように読みを優先して選択してください。
- ひらがな・カタカナで入力された単語は、その表記をそのまま出力してください。
- 出力順は入力順と同じにしてください。
- 出力は各単語の読みのみをカンマ区切りで出力してください。
- 説明、引用符、番号、改行などは一切不要です。

例：
入力：上手,宴,裏拳,燕
出力：うわて,うたげ,うらけん,つばめ

入力：")
              .Append(string.Join(",", words));
            return sb.ToString();
        }

        public string BuildWordFilteringPrompt(IReadOnlyList<string> words)
        {
            var wordList = words != null && words.Count > 0 ? string.Join(",", words) : "";
            var sb = new StringBuilder();
            sb.Append(
@"以下の単語群のうち、実在する日本語の単語のみをカンマ区切りで出力してください。存在しない単語や無意味な文字列は除外してください。
すべて存在しない場合は「null」と出力してください。
出力は抽出された単語のカンマ区切りのみとし、説明や注釈は一切不要です。

入力：")
              .Append(wordList);
            return sb.ToString();
        }

        #region 拡張 4段階
        public string BuildPlayerVersePrompt_1(IReadOnlyList<string> rhymes)
        {
            var sb = new StringBuilder();
            sb.Append(
@" 以下のライム群を用いてラップバトルのバースを生成する．バースを生成する上で，相手への攻撃，自分の誇示に用いることができるライムのみ抽出しなさい．出力は各グループとそれに含まれるライムのみにし，他の語彙を含まないこと．出力はライムを/で区切るだけにしなさい．ライムは4つにすること．5つ以上にしてはならない．

ライム群：")
              .Append(string.Join(',', rhymes));
            return sb.ToString();
        }

        public string BuildPlayerVersePrompt_2(string selectedRhymesLine)
        {
            var sb = new StringBuilder();
            sb.Append(
@"以下の単語群を用いて，ラップをしている相手を攻撃するか自分を誇示するような文章を単語一つに対して一行，合計4行で短いフレーズを生成しなさい．各行15文字以内にすること．各単語は各行の最後に配置すること．各行以外の内容は含めるな．

例：変えてやるよ人生観
持った才能まるで晋平太

ライム群：")
              .Append(selectedRhymesLine);
            return sb.ToString();
        }
        public string BuildPlayerVersePrompt_3(string selectedRhymesLine, string baseVerse)
        {
            var sb = new StringBuilder();
            sb.Append(
@" 以下の文章をラップのような文章に整えてほしい．各行を並び替えたり，適切な接続をしたりしなさい．また，次のライム郡が各行末に必ず来るようにしなさい．ただしライム群のライムが4つ未満であればその限りではない．出力は4行にすること．句読点を入れてはならない．

例：こいつは顔面チンパンジー
ラップのレベルは一般人
こいつを細切れにする俺がビンラディン

ライム郡：")
              .AppendLine(selectedRhymesLine)
              .Append("文章：")
              .Append(baseVerse);
            return sb.ToString();
        }
        public string BuildPlayerVersePrompt_4(string selectedRhymesLine, string baseVerse)
        {
            var sb = new StringBuilder();
            sb.Append(
@" 以下の文章に対して，各行の長さを15～17モーラになるようにすること．また，各行の語末には次のライム郡を必ずつけること．出力は句読点を含まない4行の文章のみにすること．

例：こいつは顔面チンパンジー
ラップのレベルは一般人
こいつを細切れにする俺がビンラディン

ライム群：")
              .AppendLine(selectedRhymesLine)
              .Append("文章：")
              .Append(baseVerse);
            return sb.ToString();
        }
        #endregion  (拡張 4段階)

        public string BuildFeedbackPrompt(IReadOnlyList<TurnData> turns)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
@"あなたはラップバトルの審査員です。
これまでの全ターンのプレイヤーの韻の踏み方やバースについて、良かった点や韻の組み合わせの工夫などを評価し、フィードバックを作成してください。
簡潔にコメントし、出力はフィードバック本文のみとしてください。

【これまでのターン】");

            for (int i = 0; i < turns.Count; i++)
            {
                sb.AppendLine($"--- ターン{i + 1} ---");
                sb.AppendLine($"相手のバース:\n{turns[i].OpponentVerse.Text}");
                sb.AppendLine($"プレイヤーが入力した韻語: {string.Join("・", turns[i].InputRhymes)}");
                sb.AppendLine($"生成されたプレイヤーのバース:\n{turns[i].GeneratedPlayerVerse.Text}");
            }

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
