using System.Collections.Generic;

namespace RhAImers.Input
{
    /// <summary>
    /// ライム入力を管理するクラス
    /// </summary>
    public class RhymeInputController
    {
        /// <summary>入力されたライム</summary>
        private readonly List<string> _rhymes = new();
        /// <summary>入力を受け付けるかどうか</summary>
        private bool _allowInput;

        /// <summary>
        /// 入力の受付を開始します．
        /// </summary>
        public void StartInput()
        {
            _allowInput = true;     // 受付開始
            _rhymes.Clear();        // 入力されたライムリストのリセット
        }

        /// <summary>
        /// ライムを追加します．
        /// </summary>
        /// <param name="word">追加するライム</param>
        public void AddRhyme(string word)
        {
            if (!_allowInput || string.IsNullOrWhiteSpace(word)) { return; }    // 入力受付中でない場合や，空白文字の場合は追加しない

            _rhymes.Add(word);      // ライムを追加
        }

        /// <summary>
        /// ライムを削除します．
        /// </summary>
        /// <param name="word">削除するライム</param>
        public void RemoveRhyme(string word)
        {
            if (!_allowInput) { return; }

            _rhymes.Remove(word);
        }

        /// <summary>
        /// 入力されたライムを返します．
        /// </summary>
        /// <returns>入力されたライム</returns>
        public IReadOnlyList<string> Submit()
        {
            _allowInput = false;
            return _rhymes.AsReadOnly();
        }
    }
}