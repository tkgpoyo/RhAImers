using UnityEngine;

namespace RhAImers.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("RhAImers/UI/Battle Panel Sound Presenter")]
    public sealed class BattlePanelSoundPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private AudioSource _audioSource;

        [Header("Panel Sounds")]
        [SerializeField] private bool _playOpponentVersePanelSound;
        [SerializeField] private AudioClip _opponentVersePanelAppearSound;
        [SerializeField] private bool _playRhymeInputPanelSound;
        [SerializeField] private AudioClip _rhymeInputPanelAppearSound;
        [SerializeField] private bool _playPlayerVersePanelSound = true;
        [SerializeField] private AudioClip _playerVersePanelAppearSound;
        [SerializeField] private float _volume = 1f;

        private void Reset()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
            TryGetComponent(out _audioSource);
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_uiManager != null)
            {
                _uiManager.BattlePanelShown += HandleBattlePanelShown;
            }
        }

        private void OnDisable()
        {
            if (_uiManager != null)
            {
                _uiManager.BattlePanelShown -= HandleBattlePanelShown;
            }
        }

        private void HandleBattlePanelShown(BattleUiPanelKind panelKind)
        {
            switch (panelKind)
            {
                case BattleUiPanelKind.OpponentVerse:
                    if (_playOpponentVersePanelSound)
                    {
                        PlayOneShot(_opponentVersePanelAppearSound);
                    }
                    break;

                case BattleUiPanelKind.RhymeInput:
                    if (_playRhymeInputPanelSound)
                    {
                        PlayOneShot(_rhymeInputPanelAppearSound);
                    }
                    break;

                case BattleUiPanelKind.PlayerVerse:
                    if (_playPlayerVersePanelSound)
                    {
                        PlayOneShot(_playerVersePanelAppearSound);
                    }
                    break;
            }
        }

        private void ResolveReferences()
        {
            if (_uiManager == null)
            {
                _uiManager = FindFirstObjectByType<UIManager>();
            }

            if (_audioSource == null)
            {
                TryGetComponent(out _audioSource);
            }
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (_audioSource == null || clip == null)
            {
                return;
            }

            _audioSource.PlayOneShot(clip, Mathf.Max(0f, _volume));
        }
    }
}
