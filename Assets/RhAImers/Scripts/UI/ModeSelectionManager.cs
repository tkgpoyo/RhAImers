using System;
using RhAImers.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    public class ModeSelectionManager : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _backButton;

        [Header("Difficulty Buttons")]
        [SerializeField] private Button _easyButton;
        [SerializeField] private Button _normalButton;
        [SerializeField] private Button _hardButton;

        public event Action StartSelected;
        public event Action BackSelected;
        public event Action<Difficulty> DifficultySelected;

        private void OnEnable()
        {
            if (_startButton != null)
            {
                _startButton.onClick.AddListener(HandleStartButtonClicked);
            }

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(HandleBackButtonClicked);
            }

            if (_easyButton != null)
            {
                _easyButton.onClick.AddListener(HandleEasyButtonClicked);
            }

            if (_normalButton != null)
            {
                _normalButton.onClick.AddListener(HandleNormalButtonClicked);
            }

            if (_hardButton != null)
            {
                _hardButton.onClick.AddListener(HandleHardButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(HandleStartButtonClicked);
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackButtonClicked);
            }

            if (_easyButton != null)
            {
                _easyButton.onClick.RemoveListener(HandleEasyButtonClicked);
            }

            if (_normalButton != null)
            {
                _normalButton.onClick.RemoveListener(HandleNormalButtonClicked);
            }

            if (_hardButton != null)
            {
                _hardButton.onClick.RemoveListener(HandleHardButtonClicked);
            }
        }

        private void HandleStartButtonClicked()
        {
            StartSelected?.Invoke();
        }

        private void HandleBackButtonClicked()
        {
            BackSelected?.Invoke();
        }

        private void HandleEasyButtonClicked()
        {
            DifficultySelected?.Invoke(Difficulty.Easy);
        }

        private void HandleNormalButtonClicked()
        {
            DifficultySelected?.Invoke(Difficulty.Normal);
        }

        private void HandleHardButtonClicked()
        {
            DifficultySelected?.Invoke(Difficulty.Hard);
        }
    }
}
