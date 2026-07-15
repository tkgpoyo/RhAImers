using System;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    public class ResultSceneManager : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _modeSelectionButton;

        public event Action ModeSelectionSelected;

        private void OnEnable()
        {
            if (_modeSelectionButton != null)
            {
                _modeSelectionButton.onClick.AddListener(HandleModeSelectionButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (_modeSelectionButton != null)
            {
                _modeSelectionButton.onClick.RemoveListener(HandleModeSelectionButtonClicked);
            }
        }

        private void HandleModeSelectionButtonClicked()
        {
            ModeSelectionSelected?.Invoke();
        }
    }
}
