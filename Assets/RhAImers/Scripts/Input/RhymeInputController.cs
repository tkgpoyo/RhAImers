using System;
using System.Collections.Generic;
using RhAImers.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.Input
{
    /// <summary>
    /// ライム入力を管理するクラス
    /// </summary>
    public class RhymeInputController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private InputField _rhymeInputField;
        [SerializeField] private Button _submitButton;
        [SerializeField] private UIManager _uiManager;

        /// <summary>入力されたライム</summary>
        private readonly List<string> _rhymes = new();

        /// <summary>入力を受け付けるかどうか</summary>
        private bool _allowInput;

        public event Action<IReadOnlyList<string>> Submitted;

        public IReadOnlyList<string> Rhymes => _rhymes.AsReadOnly();

        private void Awake()
        {
            if (_rhymeInputField != null)
            {
                _rhymeInputField.lineType = InputField.LineType.SingleLine;
            }
        }

        private void OnEnable()
        {
            if (_rhymeInputField != null)
            {
                _rhymeInputField.onEndEdit.AddListener(HandleInputEnded);
            }

            if (_submitButton != null)
            {
                _submitButton.onClick.AddListener(SubmitInput);
            }
        }

        private void OnDisable()
        {
            if (_rhymeInputField != null)
            {
                _rhymeInputField.onEndEdit.RemoveListener(HandleInputEnded);
            }

            if (_submitButton != null)
            {
                _submitButton.onClick.RemoveListener(SubmitInput);
            }
        }

        /// <summary>
        /// 入力の受付を開始します。
        /// </summary>
        public void StartInput()
        {
            _allowInput = true;
            _rhymes.Clear();

            _uiManager?.ShowInputRhymes(_rhymes);

            if (_rhymeInputField != null)
            {
                _rhymeInputField.SetTextWithoutNotify(string.Empty);
                _rhymeInputField.interactable = true;
                _rhymeInputField.Select();
                _rhymeInputField.ActivateInputField();
            }

            if (_submitButton != null)
            {
                _submitButton.interactable = true;
            }
        }

        /// <summary>
        /// ライムを追加します。
        /// </summary>
        /// <param name="word">追加するライム</param>
        public void AddRhyme(string word)
        {
            Debug.Log($"AddRhyme called. allowInput={_allowInput}, word={word}");

            if (!_allowInput || string.IsNullOrWhiteSpace(word))
            {
                Debug.Log("AddRhyme skipped.");
                return;
            }

            string trimmedWord = word.Trim();

            _rhymes.Add(trimmedWord);

            Debug.Log($"Rhyme added: {trimmedWord}, count={_rhymes.Count}");

            _uiManager?.ShowInputRhymes(_rhymes);

            ClearAndFocusInputField();
        }

        /// <summary>
        /// ライムを削除します。
        /// </summary>
        /// <param name="word">削除するライム</param>
        public void RemoveRhyme(string word)
        {
            if (!_allowInput || string.IsNullOrWhiteSpace(word))
            {
                return;
            }

            _rhymes.Remove(word.Trim());
            _uiManager?.ShowInputRhymes(_rhymes);
        }

        /// <summary>
        /// 入力されたライムを返します。
        /// </summary>
        /// <returns>入力されたライム</returns>
        public IReadOnlyList<string> Submit()
        {
            _allowInput = false;

            if (_rhymeInputField != null)
            {
                _rhymeInputField.interactable = false;
            }

            if (_submitButton != null)
            {
                _submitButton.interactable = false;
            }

            return new List<string>(_rhymes);
        }

        /// <summary>
        /// 現在の入力内容を提出します。
        /// </summary>
        public void SubmitInput()
        {
            if (!_allowInput)
            {
                return;
            }

            AddCurrentInputText();

            IReadOnlyList<string> submittedRhymes = Submit();
            Submitted?.Invoke(submittedRhymes);
        }

        private void AddCurrentInputText()
        {
            if (_rhymeInputField == null)
            {
                return;
            }

            AddRhyme(_rhymeInputField.text);
        }

        private void ClearAndFocusInputField()
        {
            if (_rhymeInputField == null)
            {
                return;
            }

            _rhymeInputField.SetTextWithoutNotify(string.Empty);
            _rhymeInputField.Select();
            _rhymeInputField.ActivateInputField();
        }

        private void HandleInputEnded(string word)
        {
            AddRhyme(word);
        }
    }
}