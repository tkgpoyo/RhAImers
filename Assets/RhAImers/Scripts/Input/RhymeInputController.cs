using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Cysharp.Threading.Tasks;
using RhAImers.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RhAImers.Input
{
    public class RhymeInputController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private InputField _rhymeInputField;
        [SerializeField] private Button _submitButton;
        [SerializeField] private UIManager _uiManager;

        [Header("Submit Event")]
        [SerializeField] private bool _notifySubmitRequested = true;

        [Header("Input Focus")]
        [SerializeField] private bool _refocusInputAfterAdd = true;
        [SerializeField] private bool _keepInputFieldFocused = true;

        [Header("Debug")]
        [SerializeField] private bool _logRhymeInputDebug = false;

        private readonly List<string> _rhymes = new();

        private bool _allowInput;
        private Coroutine _refocusCoroutine;

        private int _lastAddedFrame = -1;
        private string _lastAddedWord = string.Empty;

        public event Action SubmitRequested;
        public event Action<IReadOnlyList<string>> Submitted;
        public event Action<string> RhymeAdded;
        public event Action<string> RhymeRemoved;

        public IReadOnlyList<string> Rhymes => _rhymes.AsReadOnly();

        private void Awake()
        {
            if (_rhymeInputField != null)
            {
                _rhymeInputField.lineType = InputField.LineType.SingleLine;
            }

            if (_submitButton != null)
            {
                Navigation navigation = _submitButton.navigation;
                navigation.mode = Navigation.Mode.None;
                _submitButton.navigation = navigation;
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

            if (_uiManager != null)
            {
                _uiManager.InputRhymeRemoveAtRequested += HandleInputRhymeRemoveAtRequested;
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

            if (_uiManager != null)
            {
                _uiManager.InputRhymeRemoveAtRequested -= HandleInputRhymeRemoveAtRequested;
            }

            StopRefocusCoroutine();
        }

        private void LateUpdate()
        {
            if (!_keepInputFieldFocused)
            {
                return;
            }

            if (!_allowInput || _rhymeInputField == null || !_rhymeInputField.interactable)
            {
                return;
            }

            if (!_rhymeInputField.isFocused || IsAnotherObjectSelected())
            {
                FocusInputField();
            }
        }

        public void SetNotifySubmitRequested(bool notify)
        {
            _notifySubmitRequested = notify;
        }

        public void StartInput()
        {
            StopRefocusCoroutine();

            _allowInput = true;
            _rhymes.Clear();
            _lastAddedFrame = -1;
            _lastAddedWord = string.Empty;

            _uiManager?.ShowInputRhymes(_rhymes);

            if (_rhymeInputField != null)
            {
                _rhymeInputField.SetTextWithoutNotify(string.Empty);
                _rhymeInputField.interactable = true;
            }

            if (_submitButton != null)
            {
                _submitButton.interactable = true;
            }

            RequestFocusInputFieldNextFrame();
        }

        public void AddRhyme(string word)
        {
            TryAddRhyme(word);
        }

        public void RemoveRhyme(string word)
        {
            if (!_allowInput || string.IsNullOrWhiteSpace(word))
            {
                return;
            }

            string trimmedWord = word.Trim();
            bool removed = _rhymes.Remove(trimmedWord);

            if (!removed)
            {
                return;
            }

            _uiManager?.ShowInputRhymes(_rhymes);
            RhymeRemoved?.Invoke(trimmedWord);

            if (_refocusInputAfterAdd)
            {
                RequestFocusInputFieldNextFrame();
            }
        }

        public void RemoveRhymeAt(int index)
        {
            if (!_allowInput || index < 0 || index >= _rhymes.Count)
            {
                return;
            }

            string removedWord = _rhymes[index];
            _rhymes.RemoveAt(index);
            _uiManager?.ShowInputRhymes(_rhymes);
            RhymeRemoved?.Invoke(removedWord);

            if (_refocusInputAfterAdd)
            {
                RequestFocusInputFieldNextFrame();
            }
        }

        public IReadOnlyList<string> Submit()
        {
            _allowInput = false;
            StopRefocusCoroutine();

            if (_rhymeInputField != null)
            {
                _rhymeInputField.DeactivateInputField();
                _rhymeInputField.interactable = false;
            }

            if (_submitButton != null)
            {
                _submitButton.interactable = false;
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            return new List<string>(_rhymes);
        }

        public void SubmitInput()
        {
            if (!_allowInput)
            {
                return;
            }

            AddCurrentInputText();

            IReadOnlyList<string> submittedRhymes = Submit();

            Submitted?.Invoke(submittedRhymes);

            if (_notifySubmitRequested)
            {
                SubmitRequested?.Invoke();
            }
        }

        private void HandleInputSubmitted(string word)
        {
            TryAddRhyme(word);
        }

        private void HandleInputEnded(string word)
        {
            TryAddRhyme(word);
        }

        private void HandleInputRhymeRemoveAtRequested(int index)
        {
            RemoveRhymeAt(index);
        }

        private void AddCurrentInputText()
        {
            if (_rhymeInputField == null)
            {
                return;
            }

            TryAddRhyme(_rhymeInputField.text);
        }

        private void TryAddRhyme(string word)
        {
            if (!_allowInput || string.IsNullOrWhiteSpace(word))
            {
                _uiManager.ShowRhymeInvalidInputVibration().Forget();   // TODO: キャンセル処理諸々ちゃんと考える
                RequestFocusInputFieldNextFrame();
                return;
            }

            string trimmedWord = word.Trim();

            if (_lastAddedFrame == Time.frameCount && _lastAddedWord == trimmedWord)
            {
                _uiManager.ShowRhymeInvalidInputVibration().Forget();   // TODO: キャンセル処理諸々ちゃんと考える
                RequestFocusInputFieldNextFrame();
                return;
            }

            // 2026/07/11 ADD アルファベットが含まれる場合は除去
            if (!IsNonAlphabeticLettersOnly(trimmedWord))
            {
                _uiManager.ShowRhymeInvalidInputVibration().Forget();   // TODO: キャンセル処理諸々ちゃんと考える
                RequestFocusInputFieldNextFrame();
                return;
            }

            _lastAddedFrame = Time.frameCount;
            _lastAddedWord = trimmedWord;

            _rhymes.Add(trimmedWord);

            if (_logRhymeInputDebug)
            {
                Debug.Log($"Rhyme added: {trimmedWord}. Count={_rhymes.Count}");
            }

            _uiManager?.ShowInputRhymes(_rhymes);
            RhymeAdded?.Invoke(trimmedWord);

            ClearInputField();

            if (_refocusInputAfterAdd)
            {
                RequestFocusInputFieldNextFrame();
            }
        }

        private void ClearInputField()
        {
            if (_rhymeInputField == null)
            {
                return;
            }

            _rhymeInputField.SetTextWithoutNotify(string.Empty);
        }

        private bool IsAnotherObjectSelected()
        {
            if (EventSystem.current == null || _rhymeInputField == null)
            {
                return false;
            }

            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
            return selectedObject != null && selectedObject != _rhymeInputField.gameObject;
        }

        private void RequestFocusInputFieldNextFrame()
        {
            if (!_allowInput || _rhymeInputField == null)
            {
                return;
            }

            StopRefocusCoroutine();
            _refocusCoroutine = StartCoroutine(FocusInputFieldNextFrame());
        }

        private IEnumerator FocusInputFieldNextFrame()
        {
            yield return null;

            if (_allowInput)
            {
                FocusInputField();
            }

            _refocusCoroutine = null;
        }

        private void FocusInputField()
        {
            if (_rhymeInputField == null || !_rhymeInputField.interactable)
            {
                return;
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(_rhymeInputField.gameObject);
            }

            _rhymeInputField.Select();
            _rhymeInputField.ActivateInputField();
        }

        private void StopRefocusCoroutine()
        {
            if (_refocusCoroutine == null)
            {
                return;
            }

            StopCoroutine(_refocusCoroutine);
            _refocusCoroutine = null;
        }

        /// <summary>
        /// 入力文字列が，ASCII英字を除くUnicode上の文字のみで構成されているかを返します．
        /// </summary>
        /// <param name="word">判定する文字列</param>
        /// <returns>
        /// ひらがな，カタカナ，漢字などの文字だけならtrue，
        /// 英字，数字，記号，空白，絵文字などを含む場合はfalse
        /// </returns>
        private static bool IsNonAlphabeticLettersOnly(string word)
        {
            /* MEMO 
                C#では string を char[] で扱えるが， 文字コードを UTF-16 で扱うらしい （出典： https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-encoding-introduction ） 
                UTF-16 は 2byte または 4byte の可変長文字コードであり， 4byte の場合は2つ連続した char として扱う 
                ここで， 4byte のとき surrogate pairs によって文字を表現し， high surrogate と low surrogate に分けられる
            */
            if (string.IsNullOrEmpty(word))
            {
                return false;
            }

            // 「が」と「か + 結合濁点」などの表現を可能な限り統一する
            word = word.Normalize(NormalizationForm.FormC);

            for (int i = 0; i < word.Length;)
            {
                int codePoint;

                if (char.IsHighSurrogate(word[i]))
                {
                    if (i + 1 >= word.Length || !char.IsLowSurrogate(word[i + 1]))
                    {
                        return false;
                    }

                    codePoint = char.ConvertToUtf32(word[i], word[i + 1]);
                }
                else if (char.IsLowSurrogate(word[i]))
                {
                    return false;
                }
                else
                {
                    codePoint = word[i];
                }

                // ASCII英字を除外する
                if (codePoint is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
                {
                    return false;
                }

                UnicodeCategory category =
                    CharUnicodeInfo.GetUnicodeCategory(word, i);

                if (!IsLetterCategory(category))
                {
                    return false;
                }

                i += codePoint > 0xFFFF ? 2 : 1;
            }

            return true;

            bool IsLetterCategory(UnicodeCategory category)
            {
                return category is
                    //UnicodeCategory.UppercaseLetter or
                    //UnicodeCategory.LowercaseLetter or
                    //UnicodeCategory.TitlecaseLetter or
                    //UnicodeCategory.ModifierLetter or
                    UnicodeCategory.OtherLetter;
            }
        }
    }
}
