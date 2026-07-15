using System;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    public class StartSceneManager : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _startButton;

        public event Action StartSelected;

        private void OnEnable()
        {
            if (_startButton != null)
            {
                _startButton.onClick.AddListener(HandleStartButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(HandleStartButtonClicked);
            }
        }

        private void HandleStartButtonClicked()
        {
            StartSelected?.Invoke();
        }
    }
}
