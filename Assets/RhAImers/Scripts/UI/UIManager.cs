using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using RhAImers.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    public class UIManager : MonoBehaviour
    {
        private const string HighlightColor = "#FFD54F";

        [Header("Background UI")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Sprite _battleBackgroundSprite;
        [SerializeField] private Color _backgroundColor = Color.white;

        [Header("Battle Flow Visibility")]
        [SerializeField] private GameObject _opponentVerseGroup;
        [SerializeField] private GameObject _playerVerseGroup;
        [SerializeField] private GameObject _rhymeInputGroup;

        [Header("Verse Panel UI")]
        [SerializeField] private Image _opponentVersePanelImage;
        [SerializeField] private Image _playerVersePanelImage;
        [SerializeField] private Sprite _opponentVersePanelSprite;
        [SerializeField] private Sprite _playerVersePanelSprite;
        [SerializeField] private Color _opponentVersePanelColor = Color.white;
        [SerializeField] private Color _playerVersePanelColor = Color.white;
        [SerializeField] private bool _useSlicedVersePanels = true;

        [Header("Rhyme Input Panel UI")]
        [SerializeField] private Image _rhymeInputPanelImage;
        [SerializeField] private Sprite _rhymeInputPanelSprite;
        [SerializeField] private Color _rhymeInputPanelColor = Color.white;
        [SerializeField] private bool _useSlicedRhymeInputPanel = true;

        [Header("Rhyme Tab UI")]
        [SerializeField] private ScrollRect _inputRhymesScrollRect;
        [SerializeField] private RectTransform _inputRhymesContent;
        [SerializeField] private GameObject _inputRhymeTabTemplate;
        [SerializeField] private Text _inputRhymesEmptyText;
        [SerializeField] private string _rhymeTabWordTextName = "WordText";
        [SerializeField] private string _rhymeTabRemoveButtonName = "RemoveButton";

        [Header("Battle UI")]
        [SerializeField] private Text _opponentVerseText;
        [SerializeField] private Text _inputTimerText;
        [SerializeField] private Text _inputRhymesText;
        [SerializeField] private Text _generatedVerseText;
        [SerializeField] private Text _resultText;
        [SerializeField] private Text _statusText;

        [Header("Result UI")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private Button _retryButton;

        private readonly List<GameObject> _inputRhymeTabInstances = new();

        public event Action RetrySelected;
        public event Action<int> InputRhymeRemoveAtRequested;

        private void Awake()
        {
            ApplyBattleBackground();
            ApplyVersePanels();
            ApplyRhymeInputPanel();
            PrepareRhymeTabTemplate();
            ShowInputPhase();
            SetResultVisible(false);
        }

        private void OnEnable()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.AddListener(HandleRetryButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveListener(HandleRetryButtonClicked);
            }
        }

        public void ShowTitle()
        {
            SetStatus("Title");
            SetResultVisible(false);
        }

        public void ShowModeSelect()
        {
            SetStatus("Mode Select");
            SetResultVisible(false);
        }

        public void ShowOpponentVerse(Verse verse)
        {
            ShowInputPhase();
            SetStatus("Opponent Verse");
            SetResultVisible(false);

            string text = verse == null ? string.Empty : FormatVerseText(verse.Text);
            SetText(_opponentVerseText, text);
        }

        public void ShowInputTimer(int sec)
        {
            ShowInputPhase();
            UpdateInputTimer(sec);
        }

        public void UpdateInputTimer(int sec)
        {
            SetText(_inputTimerText, FormatTimer(sec));
        }

        public void ShowInputRhymes(IReadOnlyList<string> rhymes)
        {
            ShowInputPhase();
            UpdateInputRhymesTextFallback(rhymes);
            RebuildInputRhymeTabs(rhymes);
        }

        public void ShowGeneratedVerse(Verse verse)
        {
            ShowPlayerVersePhase();
            SetStatus("Generated Verse");
            SetResultVisible(false);

            string text = verse == null ? string.Empty : FormatVerseText(verse.Text);
            SetText(_generatedVerseText, text);
        }

        public void ShowResult(BattleResult result)
        {
            HideBattlePhaseGroups();
            SetStatus("Result");
            SetResultVisible(true);

            string resultText = BuildResultText(result);
            SetText(_resultText, resultText);
        }

        public void ShowOpponentVerseLoading()
        {
            ShowInputPhase();
            SetStatus("Opponent Verse Loading");
            SetResultVisible(false);
            SetText(_opponentVerseText, "相手のバース生成中...");
        }

        public void ShowGenerationLoading()
        {
            ShowPlayerVersePhase();
            SetStatus("Verse Generation Loading");
            SetResultVisible(false);
            SetText(_generatedVerseText, "あなたのバース生成中...");
        }

        public void ShowScoringLoading()
        {
            HideBattlePhaseGroups();
            SetStatus("Scoring Loading");
            SetResultVisible(false);
            SetText(_resultText, "採点中...");
        }

        public void ShowInputPhase()
        {
            SetGroupVisible(_opponentVerseGroup, _opponentVersePanelImage, true);
            SetGroupVisible(_rhymeInputGroup, _rhymeInputPanelImage, true);
            SetGroupVisible(_playerVerseGroup, _playerVersePanelImage, false);
        }

        public void ShowPlayerVersePhase()
        {
            SetGroupVisible(_opponentVerseGroup, _opponentVersePanelImage, false);
            SetGroupVisible(_rhymeInputGroup, _rhymeInputPanelImage, false);
            SetGroupVisible(_playerVerseGroup, _playerVersePanelImage, true);
        }

        public void HideBattlePhaseGroups()
        {
            SetGroupVisible(_opponentVerseGroup, _opponentVersePanelImage, false);
            SetGroupVisible(_rhymeInputGroup, _rhymeInputPanelImage, false);
            SetGroupVisible(_playerVerseGroup, _playerVersePanelImage, false);
        }

        private void HandleRetryButtonClicked()
        {
            RetrySelected?.Invoke();
        }

        private void ApplyBattleBackground()
        {
            if (_backgroundImage == null)
            {
                return;
            }

            if (_battleBackgroundSprite != null)
            {
                _backgroundImage.sprite = _battleBackgroundSprite;
            }

            _backgroundImage.color = _backgroundColor;
            _backgroundImage.type = Image.Type.Simple;
            _backgroundImage.raycastTarget = false;

            _backgroundImage.transform.SetAsFirstSibling();
        }

        private void ApplyVersePanels()
        {
            ApplyPanelImage(
                _opponentVersePanelImage,
                _opponentVersePanelSprite,
                _opponentVersePanelColor,
                _useSlicedVersePanels
            );

            ApplyPanelImage(
                _playerVersePanelImage,
                _playerVersePanelSprite,
                _playerVersePanelColor,
                _useSlicedVersePanels
            );
        }

        private void ApplyRhymeInputPanel()
        {
            ApplyPanelImage(
                _rhymeInputPanelImage,
                _rhymeInputPanelSprite,
                _rhymeInputPanelColor,
                _useSlicedRhymeInputPanel
            );

            if (_inputRhymesScrollRect != null)
            {
                _inputRhymesScrollRect.horizontal = false;
                _inputRhymesScrollRect.vertical = true;
                _inputRhymesScrollRect.movementType = ScrollRect.MovementType.Clamped;
            }
        }

        private void ApplyPanelImage(Image panelImage, Sprite panelSprite, Color panelColor, bool useSliced)
        {
            if (panelImage == null)
            {
                return;
            }

            if (panelSprite != null)
            {
                panelImage.sprite = panelSprite;
            }

            panelImage.color = panelColor;
            panelImage.raycastTarget = false;
            panelImage.type = useSliced ? Image.Type.Sliced : Image.Type.Simple;
            panelImage.fillCenter = true;
        }

        private void PrepareRhymeTabTemplate()
        {
            if (_inputRhymeTabTemplate != null)
            {
                _inputRhymeTabTemplate.SetActive(false);
            }

            if (_inputRhymesEmptyText != null)
            {
                _inputRhymesEmptyText.gameObject.SetActive(true);
            }
        }

        private void UpdateInputRhymesTextFallback(IReadOnlyList<string> rhymes)
        {
            if (_inputRhymesText == null)
            {
                return;
            }

            if (rhymes == null || rhymes.Count == 0)
            {
                _inputRhymesText.text = "入力済みライム：なし";
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("入力済みライム：");

            for (int i = 0; i < rhymes.Count; i++)
            {
                builder.AppendLine($"{i + 1}. {rhymes[i]}");
            }

            _inputRhymesText.text = builder.ToString();
        }

        private void RebuildInputRhymeTabs(IReadOnlyList<string> rhymes)
        {
            ClearInputRhymeTabs();

            bool hasRhymes = rhymes != null && rhymes.Count > 0;

            if (_inputRhymesEmptyText != null)
            {
                _inputRhymesEmptyText.gameObject.SetActive(!hasRhymes);
            }

            if (!hasRhymes || _inputRhymeTabTemplate == null || _inputRhymesContent == null)
            {
                return;
            }

            for (int i = 0; i < rhymes.Count; i++)
            {
                int rhymeIndex = i;
                string rhymeWord = rhymes[i];

                GameObject tab = Instantiate(_inputRhymeTabTemplate, _inputRhymesContent);
                tab.name = $"InputRhymeTab_{i + 1}";
                tab.transform.localScale = Vector3.one;
                tab.SetActive(true);

                Text wordText = FindText(tab.transform, _rhymeTabWordTextName);
                if (wordText != null)
                {
                    wordText.supportRichText = true;
                    wordText.text = rhymeWord;
                }

                Button removeButton = FindButton(tab.transform, _rhymeTabRemoveButtonName);
                if (removeButton != null)
                {
                    removeButton.onClick.RemoveAllListeners();
                    removeButton.onClick.AddListener(() => InputRhymeRemoveAtRequested?.Invoke(rhymeIndex));
                    removeButton.interactable = true;
                }

                _inputRhymeTabInstances.Add(tab);
            }

            Canvas.ForceUpdateCanvases();

            if (_inputRhymesScrollRect != null)
            {
                _inputRhymesScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void ClearInputRhymeTabs()
        {
            for (int i = 0; i < _inputRhymeTabInstances.Count; i++)
            {
                GameObject tab = _inputRhymeTabInstances[i];

                if (tab == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(tab);
                }
                else
                {
                    DestroyImmediate(tab);
                }
            }

            _inputRhymeTabInstances.Clear();
        }

        private Text FindText(Transform root, string preferredName)
        {
            Transform preferred = FindChildRecursive(root, preferredName);
            if (preferred != null && preferred.TryGetComponent(out Text preferredText))
            {
                return preferredText;
            }

            return root.GetComponentInChildren<Text>(true);
        }

        private Button FindButton(Transform root, string preferredName)
        {
            Transform preferred = FindChildRecursive(root, preferredName);
            if (preferred != null && preferred.TryGetComponent(out Button preferredButton))
            {
                return preferredButton;
            }

            return root.GetComponentInChildren<Button>(true);
        }

        private Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child.name == childName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private void SetGroupVisible(GameObject group, Component fallbackComponent, bool visible)
        {
            if (group != null)
            {
                group.SetActive(visible);
                return;
            }

            if (fallbackComponent != null)
            {
                fallbackComponent.gameObject.SetActive(visible);
            }
        }

        private void SetResultVisible(bool visible)
        {
            if (_resultPanel != null)
            {
                _resultPanel.SetActive(visible);

                if (visible)
                {
                    _resultPanel.transform.SetAsLastSibling();
                }
            }

            if (_retryButton != null)
            {
                _retryButton.gameObject.SetActive(visible);
                _retryButton.interactable = visible;
            }
        }

        private string BuildResultText(BattleResult result)
        {
            object resultObject = result;
            string resultDetail = resultObject?.ToString();

            string defaultTypeName = typeof(BattleResult).ToString();
            string defaultShortTypeName = typeof(BattleResult).Name;

            if (string.IsNullOrWhiteSpace(resultDetail)
                || resultDetail == defaultTypeName
                || resultDetail == defaultShortTypeName)
            {
                return "全ターン終了\nバトルが終了しました";
            }

            return $"全ターン終了\nバトルが終了しました\n\n{resultDetail}";
        }

        private void SetStatus(string status)
        {
            SetText(_statusText, status);
        }

        private void SetText(Text target, string value)
        {
            if (target == null)
            {
                return;
            }

            target.supportRichText = true;
            target.text = value;
        }

        private string FormatTimer(int sec)
        {
            int clampedSec = Mathf.Max(0, sec);
            int minutes = clampedSec / 60;
            int seconds = clampedSec % 60;

            return $"{minutes:00}:{seconds:00}";
        }

        private string FormatVerseText(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
            {
                return string.Empty;
            }

            string text = EscapeRichText(rawText);

            text = Regex.Replace(
                text,
                @"\[\[(.+?)\]\]",
                $"<b><color={HighlightColor}>$1</color></b>"
            );

            text = Regex.Replace(
                text,
                @"【(.+?)】",
                $"<b><color={HighlightColor}>$1</color></b>"
            );

            return text;
        }

        private string EscapeRichText(string text)
        {
            return text
                .Replace("<", "＜")
                .Replace(">", "＞");
        }
    }
}
