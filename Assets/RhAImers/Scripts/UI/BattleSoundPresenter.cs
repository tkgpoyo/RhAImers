using RhAImers.Input;
using UnityEngine;

namespace RhAImers.UI
{
    public class BattleSoundPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private RhymeInputController _rhymeInputController;
        [SerializeField] private AudioSource _audioSource;

        [Header("Start Signal")]
        [SerializeField] private AudioClip _battleStartSignalClip;

        [Header("Panel Show")]
        [SerializeField] private AudioClip _opponentVersePanelShowClip;
        [SerializeField] private AudioClip _playerVersePanelShowClip;
        [SerializeField] private AudioClip _rhymeInputPanelShowClip;

        [Header("Rhyme")]
        [SerializeField] private AudioClip _rhymeAddedClip;
        [SerializeField] private AudioClip _rhymeRemovedClip;

        [Header("Verse Line")]
        [SerializeField] private AudioClip _verseLineShownClip;

        [Header("Player Verse Impact")]
        [SerializeField] private AudioClip _playerVerseImpactClip;

        [Header("Result")]
        [SerializeField] private AudioClip _allTurnsFinishedClip;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _startSignalVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _panelShowVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _rhymeVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _verseLineVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _playerVerseImpactVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _resultVolume = 1f;

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.playOnAwake = false;
        }

        private void OnEnable()
        {
            if (_uiManager != null)
            {
                _uiManager.BattleStartSignalShown += HandleBattleStartSignalShown;
                _uiManager.BattlePanelShown += HandleBattlePanelShown;
                _uiManager.BattleVerseLineShown += HandleBattleVerseLineShown;
                _uiManager.PlayerVerseImpactOccurred += HandlePlayerVerseImpactOccurred;
                _uiManager.BattleResultShown += HandleBattleResultShown;
            }

            if (_rhymeInputController != null)
            {
                _rhymeInputController.RhymeAdded += HandleRhymeAdded;
                _rhymeInputController.RhymeRemoved += HandleRhymeRemoved;
            }
        }

        private void OnDisable()
        {
            if (_uiManager != null)
            {
                _uiManager.BattleStartSignalShown -= HandleBattleStartSignalShown;
                _uiManager.BattlePanelShown -= HandleBattlePanelShown;
                _uiManager.BattleVerseLineShown -= HandleBattleVerseLineShown;
                _uiManager.PlayerVerseImpactOccurred -= HandlePlayerVerseImpactOccurred;
                _uiManager.BattleResultShown -= HandleBattleResultShown;
            }

            if (_rhymeInputController != null)
            {
                _rhymeInputController.RhymeAdded -= HandleRhymeAdded;
                _rhymeInputController.RhymeRemoved -= HandleRhymeRemoved;
            }
        }

        private void HandleBattleStartSignalShown()
        {
            PlayOneShot(_battleStartSignalClip, _startSignalVolume);
        }

        private void HandleBattlePanelShown(BattleUiPanelKind panelKind)
        {
            switch (panelKind)
            {
                case BattleUiPanelKind.OpponentVerse:
                    PlayOneShot(_opponentVersePanelShowClip, _panelShowVolume);
                    break;

                case BattleUiPanelKind.PlayerVerse:
                    PlayOneShot(_playerVersePanelShowClip, _panelShowVolume);
                    break;

                case BattleUiPanelKind.RhymeInput:
                    PlayOneShot(_rhymeInputPanelShowClip, _panelShowVolume);
                    break;
            }
        }

        private void HandleBattleVerseLineShown(BattleUiPanelKind panelKind)
        {
            PlayOneShot(_verseLineShownClip, _verseLineVolume);
        }

        private void HandlePlayerVerseImpactOccurred()
        {
            PlayOneShot(_playerVerseImpactClip, _playerVerseImpactVolume);
        }

        private void HandleRhymeAdded(string rhyme)
        {
            PlayOneShot(_rhymeAddedClip, _rhymeVolume);
        }

        private void HandleRhymeRemoved(string rhyme)
        {
            PlayOneShot(_rhymeRemovedClip, _rhymeVolume);
        }

        private void HandleBattleResultShown()
        {
            PlayOneShot(_allTurnsFinishedClip, _resultVolume);
        }

        private void PlayOneShot(AudioClip clip, float categoryVolume)
        {
            if (_audioSource == null || clip == null)
            {
                return;
            }

            _audioSource.PlayOneShot(clip, Mathf.Clamp01(_masterVolume * categoryVolume));
        }
    }
}
