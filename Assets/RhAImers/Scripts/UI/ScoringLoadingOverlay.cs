using System.Collections;
using RhAImers.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("RhAImers/UI/Scoring Loading Overlay")]
    public sealed class ScoringLoadingOverlay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private RectTransform _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private RectTransform _spinnerRoot;
        [SerializeField] private Image _spinnerImage;

        [Header("Auto Build")]
        [SerializeField] private bool _autoFindManagers = true;
        [SerializeField] private bool _autoCreateVisuals = true;
        [SerializeField] private bool _keepRootActiveWhenHidden = true;

        [Header("Visibility")]
        [SerializeField] private bool _manualControlOnly = true;
        [SerializeField] private bool _showWhenGameStateIsScoring = false;
        [SerializeField] private bool _hideWhenBattleResultIsShown = false;
        [SerializeField] private bool _blockInputWhileVisible = true;
        [SerializeField] private float _fadeInSeconds = 0.25f;
        [SerializeField] private float _fadeOutSeconds = 0.2f;
        [SerializeField] private float _minimumVisibleSeconds = 0.55f;

        [Header("Text")]
        [SerializeField] private string _message = "スコア計算中";
        [SerializeField] private Color _messageColor = Color.white;
        [SerializeField] private float _messageFontSize = 42f;
        [SerializeField] private bool _animateDots = true;
        [SerializeField] private float _dotIntervalSeconds = 0.32f;
        [SerializeField] private int _maxDotCount = 3;

        [Header("Background")]
        [SerializeField] private Color _backgroundColor = Color.black;

        [Header("Spinner")]
        [SerializeField] private bool _showSpinner = true;
        [SerializeField] private Sprite _spinnerSprite;
        [SerializeField] private Vector2 _spinnerSize = new Vector2(92f, 92f);
        [SerializeField] private Vector2 _spinnerAnchoredPosition = new Vector2(0f, -86f);
        [SerializeField] private Color _spinnerColor = new Color(0.22f, 0.9f, 1f, 0.9f);
        [SerializeField] private float _spinnerRotationSpeed = -180f;

        private UIManager _subscribedUiManager;
        private Coroutine _visibilityCoroutine;
        private Sprite _generatedSpinnerSprite;
        private float _visibleStartedAt;
        private float _dotStartedAt;
        private bool _targetVisible;
        private bool _suppressedAfterBattleResult;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            PrepareVisuals();
            ApplyHiddenImmediate();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToUiManager();
        }

        private void OnDisable()
        {
            UnsubscribeFromUiManager();
            StopVisibilityCoroutine();
        }

        private void OnDestroy()
        {
            DestroyGeneratedSprite();
        }

        private void Update()
        {
            ResolveManagersIfNeeded();
            SubscribeToUiManager();

            if (!_manualControlOnly && _showWhenGameStateIsScoring)
            {
                UpdateVisibilityFromGameState();
            }

            if (_targetVisible || GetCurrentAlpha() > 0.001f)
            {
                UpdateLoadingVisuals();
            }
        }

        [ContextMenu("Show")]
        public void Show()
        {
            ResolveReferences();
            PrepareVisuals();

            if (_targetVisible)
            {
                return;
            }

            _targetVisible = true;
            _visibleStartedAt = Time.unscaledTime;
            _dotStartedAt = Time.unscaledTime;

            if (_root != null)
            {
                _root.gameObject.SetActive(true);
                _root.SetAsLastSibling();
            }

            StartFade(GetCurrentAlpha(), 1f, _fadeInSeconds, deactivateAfterFade: false);
        }

        [ContextMenu("Hide")]
        public void Hide()
        {
            if (!_targetVisible && GetCurrentAlpha() <= 0.001f)
            {
                ApplyHiddenImmediate();
                return;
            }

            _targetVisible = false;

            float visibleSeconds = Time.unscaledTime - _visibleStartedAt;
            float delay = Mathf.Max(0f, _minimumVisibleSeconds - visibleSeconds);
            StartCoroutineAfterDelay(delay, GetCurrentAlpha(), 0f, _fadeOutSeconds, deactivateAfterFade: true);
        }

        [ContextMenu("Show Immediate")]
        public void ShowImmediate()
        {
            ResolveReferences();
            PrepareVisuals();
            _targetVisible = true;
            _visibleStartedAt = Time.unscaledTime;
            _dotStartedAt = Time.unscaledTime;

            if (_root != null)
            {
                _root.gameObject.SetActive(true);
                _root.SetAsLastSibling();
            }

            SetAlpha(1f);
            SetInputBlocked(_blockInputWhileVisible);
        }

        [ContextMenu("Hide Immediate")]
        public void HideImmediate()
        {
            _targetVisible = false;
            ApplyHiddenImmediate();
        }

        private void UpdateVisibilityFromGameState()
        {
            bool isScoring = _gameManager != null && _gameManager.CurrentState == GameState.Scoring;

            if (!isScoring)
            {
                _suppressedAfterBattleResult = false;
            }

            bool shouldShow = isScoring && !_suppressedAfterBattleResult;

            if (shouldShow && !_targetVisible)
            {
                Show();
            }
            else if (!shouldShow && _targetVisible)
            {
                Hide();
            }
        }

        private void HandleBattleResultShown()
        {
            if (_manualControlOnly)
            {
                return;
            }

            if (!_hideWhenBattleResultIsShown)
            {
                return;
            }

            _suppressedAfterBattleResult = true;
            Hide();
        }

        private void ResolveReferences()
        {
            ResolveManagersIfNeeded();

            if (_root == null)
            {
                _root = transform as RectTransform;
            }

            if (_root == null && _autoCreateVisuals)
            {
                _root = CreateRootUnderCanvas();
            }

            if (_root == null)
            {
                return;
            }

            StretchToFullScreen(_root);

            if (_canvasGroup == null)
            {
                _canvasGroup = _root.GetComponent<CanvasGroup>();
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = _root.gameObject.AddComponent<CanvasGroup>();
            }

            if (_backgroundImage == null)
            {
                _backgroundImage = _root.GetComponent<Image>();
            }

            if (_backgroundImage == null)
            {
                _backgroundImage = _root.gameObject.AddComponent<Image>();
            }
        }

        private void ResolveManagersIfNeeded()
        {
            if (!_autoFindManagers)
            {
                return;
            }

            if (_gameManager == null)
            {
                _gameManager = FindFirstObjectByType<GameManager>();
            }

            if (_uiManager == null)
            {
                _uiManager = FindFirstObjectByType<UIManager>();
            }
        }

        private RectTransform CreateRootUnderCanvas()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                return null;
            }

            var rootObject = new GameObject(
                "ScoringLoadingOverlay",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image)
            );

            rootObject.transform.SetParent(canvas.transform, false);
            return rootObject.GetComponent<RectTransform>();
        }

        private void PrepareVisuals()
        {
            if (_root == null)
            {
                return;
            }

            _root.SetAsLastSibling();
            StretchToFullScreen(_root);

            if (_backgroundImage != null)
            {
                _backgroundImage.color = _backgroundColor;
                _backgroundImage.raycastTarget = false;
            }

            if (_autoCreateVisuals)
            {
                CreateMessageTextIfNeeded();
                CreateSpinnerIfNeeded();
            }

            ApplyMessageStyle();
            ApplySpinnerStyle();
            UpdateLoadingVisuals();
        }

        private void CreateMessageTextIfNeeded()
        {
            if (_messageText != null || _root == null)
            {
                return;
            }

            Transform existing = _root.Find("MessageText");
            if (existing != null)
            {
                _messageText = existing.GetComponent<TMP_Text>();
            }

            if (_messageText != null)
            {
                return;
            }

            var textObject = new GameObject("MessageText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(_root, false);
            _messageText = textObject.GetComponent<TextMeshProUGUI>();
        }

        private void CreateSpinnerIfNeeded()
        {
            if (!_showSpinner || _spinnerImage != null || _root == null)
            {
                return;
            }

            Transform existing = _root.Find("LoadingSpinner");
            if (existing != null)
            {
                _spinnerRoot = existing as RectTransform;
                _spinnerImage = existing.GetComponent<Image>();
            }

            if (_spinnerImage != null)
            {
                return;
            }

            var spinnerObject = new GameObject("LoadingSpinner", typeof(RectTransform), typeof(Image));
            spinnerObject.transform.SetParent(_root, false);
            _spinnerRoot = spinnerObject.GetComponent<RectTransform>();
            _spinnerImage = spinnerObject.GetComponent<Image>();
        }

        private void ApplyMessageStyle()
        {
            if (_messageText == null)
            {
                return;
            }

            RectTransform rectTransform = _messageText.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(900f, 110f);

            _messageText.alignment = TextAlignmentOptions.Center;
            _messageText.color = _messageColor;
            _messageText.fontSize = Mathf.Max(1f, _messageFontSize);
            _messageText.raycastTarget = false;
        }

        private void ApplySpinnerStyle()
        {
            if (_spinnerImage == null)
            {
                return;
            }

            if (_spinnerRoot == null)
            {
                _spinnerRoot = _spinnerImage.rectTransform;
            }

            _spinnerRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _spinnerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _spinnerRoot.pivot = new Vector2(0.5f, 0.5f);
            _spinnerRoot.anchoredPosition = _spinnerAnchoredPosition;
            _spinnerRoot.sizeDelta = _spinnerSize;

            _spinnerImage.sprite = GetSpinnerSprite();
            _spinnerImage.color = _spinnerColor;
            _spinnerImage.preserveAspect = true;
            _spinnerImage.raycastTarget = false;
            _spinnerImage.gameObject.SetActive(_showSpinner);
        }

        private void UpdateLoadingVisuals()
        {
            if (_messageText != null)
            {
                _messageText.text = BuildMessage();
            }

            if (_spinnerRoot != null && _showSpinner)
            {
                float angle = _spinnerRotationSpeed * Time.unscaledDeltaTime;
                _spinnerRoot.Rotate(0f, 0f, angle);
            }
        }

        private string BuildMessage()
        {
            if (!_animateDots)
            {
                return _message;
            }

            int maxDots = Mathf.Max(1, _maxDotCount);
            float interval = Mathf.Max(0.01f, _dotIntervalSeconds);
            int dotCount = Mathf.FloorToInt((Time.unscaledTime - _dotStartedAt) / interval) % (maxDots + 1);

            return _message + new string('.', dotCount);
        }

        private void SubscribeToUiManager()
        {
            if (_subscribedUiManager == _uiManager)
            {
                return;
            }

            UnsubscribeFromUiManager();

            if (_uiManager == null)
            {
                return;
            }

            _uiManager.BattleResultShown += HandleBattleResultShown;
            _subscribedUiManager = _uiManager;
        }

        private void UnsubscribeFromUiManager()
        {
            if (_subscribedUiManager == null)
            {
                return;
            }

            _subscribedUiManager.BattleResultShown -= HandleBattleResultShown;
            _subscribedUiManager = null;
        }

        private void StartCoroutineAfterDelay(float delay, float fromAlpha, float toAlpha, float duration, bool deactivateAfterFade)
        {
            StopVisibilityCoroutine();
            _visibilityCoroutine = StartCoroutine(DelayThenFadeRoutine(delay, fromAlpha, toAlpha, duration, deactivateAfterFade));
        }

        private IEnumerator DelayThenFadeRoutine(float delay, float fromAlpha, float toAlpha, float duration, bool deactivateAfterFade)
        {
            if (delay > 0f)
            {
                float elapsedDelay = 0f;

                while (elapsedDelay < delay)
                {
                    elapsedDelay += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            yield return FadeRoutine(fromAlpha, toAlpha, duration, deactivateAfterFade);
        }

        private void StartFade(float fromAlpha, float toAlpha, float duration, bool deactivateAfterFade)
        {
            StopVisibilityCoroutine();
            _visibilityCoroutine = StartCoroutine(FadeRoutine(fromAlpha, toAlpha, duration, deactivateAfterFade));
        }

        private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration, bool deactivateAfterFade)
        {
            if (_canvasGroup == null)
            {
                yield break;
            }

            SetInputBlocked(_blockInputWhileVisible && toAlpha > 0f);

            if (duration <= 0f)
            {
                SetAlpha(toAlpha);
                FinishFade(toAlpha, deactivateAfterFade);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseInOutCubic(t);
                SetAlpha(Mathf.Lerp(fromAlpha, toAlpha, eased));
                yield return null;
            }

            SetAlpha(toAlpha);
            FinishFade(toAlpha, deactivateAfterFade);
        }

        private void FinishFade(float alpha, bool deactivateAfterFade)
        {
            SetInputBlocked(_blockInputWhileVisible && alpha > 0.001f);

            if (deactivateAfterFade && !_keepRootActiveWhenHidden && _root != null)
            {
                _root.gameObject.SetActive(false);
            }

            _visibilityCoroutine = null;
        }

        private void StopVisibilityCoroutine()
        {
            if (_visibilityCoroutine == null)
            {
                return;
            }

            StopCoroutine(_visibilityCoroutine);
            _visibilityCoroutine = null;
        }

        private void ApplyHiddenImmediate()
        {
            SetAlpha(0f);
            SetInputBlocked(false);

            if (!_keepRootActiveWhenHidden && _root != null && _root.gameObject != gameObject)
            {
                _root.gameObject.SetActive(false);
            }
        }

        private void SetAlpha(float alpha)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.Clamp01(alpha);
            }
        }

        private float GetCurrentAlpha()
        {
            return _canvasGroup != null ? _canvasGroup.alpha : 0f;
        }

        private void SetInputBlocked(bool blocked)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = blocked;
        }

        private void StretchToFullScreen(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private Sprite GetSpinnerSprite()
        {
            if (_spinnerSprite != null)
            {
                return _spinnerSprite;
            }

            if (_generatedSpinnerSprite == null)
            {
                _generatedSpinnerSprite = CreateGeneratedSpinnerSprite(128);
            }

            return _generatedSpinnerSprite;
        }

        private Sprite CreateGeneratedSpinnerSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "GeneratedScoringLoadingSpinner_Texture";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = ((x + 0.5f) / size - 0.5f) * 2f;
                    float dy = ((y + 0.5f) / size - 0.5f) * 2f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    float ring = 1f - Mathf.Clamp01(Mathf.Abs(distance - 0.72f) / 0.055f);
                    float gap = Mathf.InverseLerp(-2.2f, 2.6f, angle);
                    float alpha = Mathf.Clamp01(ring * gap);

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private void DestroyGeneratedSprite()
        {
            if (_generatedSpinnerSprite == null)
            {
                return;
            }

            Texture2D texture = _generatedSpinnerSprite.texture;
            Destroy(_generatedSpinnerSprite);

            if (texture != null)
            {
                Destroy(texture);
            }

            _generatedSpinnerSprite = null;
        }

        private float EaseInOutCubic(float value)
        {
            value = Mathf.Clamp01(value);

            if (value < 0.5f)
            {
                return 4f * value * value * value;
            }

            float inverse = -2f * value + 2f;
            return 1f - inverse * inverse * inverse / 2f;
        }
    }
}
