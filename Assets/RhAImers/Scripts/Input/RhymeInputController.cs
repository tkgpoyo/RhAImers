using System;
using System.Collections;
using System.Collections.Generic;
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

        private readonly List<string> _rhymes = new();

        private bool _allowInput;
        private Coroutine _refocusCoroutine;

        public event Action SubmitRequested;
        public event Action<IReadOnlyList<string>> Submitted;

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

            StopRefocusCoroutine();
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
            if (!_allowInput || string.IsNullOrWhiteSpace(word))
            {
                return;
            }

            _rhymes.Add(word.Trim());
            _uiManager?.ShowInputRhymes(_rhymes);

            ClearInputField();

            if (_refocusInputAfterAdd)
            {
                RequestFocusInputFieldNextFrame();
            }
        }

        public void RemoveRhyme(string word)
        {
            if (!_allowInput || string.IsNullOrWhiteSpace(word))
            {
                return;
            }

            _rhymes.Remove(word.Trim());
            _uiManager?.ShowInputRhymes(_rhymes);
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

        private void HandleInputEnded(string word)
        {
            if (!_allowInput)
            {
                return;
            }

            AddRhyme(word);
        }

        private void AddCurrentInputText()
        {
            if (_rhymeInputField == null)
            {
                return;
            }

            AddRhyme(_rhymeInputField.text);
        }

        private void ClearInputField()
        {
            if (_rhymeInputField == null)
            {
                return;
            }

            _rhymeInputField.SetTextWithoutNotify(string.Empty);
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
    }
}
