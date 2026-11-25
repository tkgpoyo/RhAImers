using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    public sealed class TutorialDialogueView : MonoBehaviour
    {
        public TMP_Text titleText;
        public TMP_Text bodyText;
        public TMP_Text progressText;
        public TMP_Text nextLabel;
        public Button nextButton;
        public Button backButton;
        public Button exitButton;
        public RectTransform portrait;
        public RectTransform speechFrame;
        public Button inspectButton;
        public TMP_Text inspectLabel;
        public RawImage nextArtwork;
        public Texture nextButtonTexture;
        public Rect nextButtonUv = new Rect(0f, 0f, 1f, 1f);
        public Texture finalButtonTexture;
        public Rect finalButtonUv = new Rect(0f, 0f, 1f, 1f);

        private RectTransform _focus;
        private int _screenWidth;
        private int _screenHeight;
        private bool _canInspect;
        private bool _reading;
        private readonly Vector3[] _focusCorners = new Vector3[4];
        private Vector3 _lastFocusMin;
        private Vector3 _lastFocusMax;

        public event Action NextRequested;
        public event Action BackRequested;
        public event Action ExitRequested;

        private void OnEnable()
        {
            nextButton.onClick.AddListener(Next);
            backButton.onClick.AddListener(Back);
            exitButton.onClick.AddListener(Exit);
            if (inspectButton != null) inspectButton.onClick.AddListener(ToggleReading);
        }

        private void OnDisable()
        {
            nextButton.onClick.RemoveListener(Next);
            backButton.onClick.RemoveListener(Back);
            exitButton.onClick.RemoveListener(Exit);
            if (inspectButton != null) inspectButton.onClick.RemoveListener(ToggleReading);
        }

        private void Next() => NextRequested?.Invoke();
        private void Back() => BackRequested?.Invoke();
        private void Exit() => ExitRequested?.Invoke();

        public void Show(string body, int index, int count, bool canAdvance, bool canGoBack)
        {
            _reading = false;
            SetExplanationVisible(true);
            titleText.text = string.Empty;
            bodyText.text = body;
            progressText.text = $"{index + 1:00} / {count:00}";
            backButton.interactable = canGoBack;
            SetReady(canAdvance, index == count - 1 ? "モード選択へ" : "次へ");
        }

        public void SetReady(bool ready, string label = "次へ")
        {
            nextButton.interactable = ready;
            nextLabel.text = ready ? label : "操作待ち";
            bool final = ready && label == "モード選択へ";
            if (nextArtwork != null)
            {
                nextArtwork.texture = final ? finalButtonTexture : nextButtonTexture;
                nextArtwork.uvRect = final ? finalButtonUv : nextButtonUv;
                nextLabel.gameObject.SetActive(final);
            }
        }

        public void PlaceAwayFrom(RectTransform target)
        {
            _focus = target;
            _canInspect = target != null && (target.name == "OpponentVersePanel" || target.name == "PlayerVersePanel");
            _reading = false;
            SetExplanationVisible(!_reading);
            LayoutGuide();
        }

        private void ToggleReading()
        {
            _reading = !_reading;
            SetExplanationVisible(!_reading);
            LayoutGuide();
        }

        private void SetExplanationVisible(bool visible)
        {
            portrait.gameObject.SetActive(visible);
            speechFrame.gameObject.SetActive(visible);
            titleText.gameObject.SetActive(false);
            bodyText.gameObject.SetActive(visible);
            progressText.gameObject.SetActive(visible);
            backButton.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            bool moved = false;
            if (_focus != null)
            {
                _focus.GetWorldCorners(_focusCorners);
                moved = (_focusCorners[0] - _lastFocusMin).sqrMagnitude > .25f
                    || (_focusCorners[2] - _lastFocusMax).sqrMagnitude > .25f;
                _lastFocusMin = _focusCorners[0];
                _lastFocusMax = _focusCorners[2];
            }
            if (moved || _screenWidth != Screen.width || _screenHeight != Screen.height) LayoutGuide();
        }

        private void LayoutGuide()
        {
            var root = (RectTransform)transform;
            var parent = (RectTransform)root.parent;
            Rect bounds = parent.rect;
            if (inspectButton != null) inspectButton.gameObject.SetActive(_canInspect);
            if (inspectLabel != null) inspectLabel.text = _reading ? "解説を見る" : "バースを読む";
            GetComponent<Image>().raycastTarget = !_reading;
            if (_reading)
            {
                SetRect(root, new Vector2(.5f, .5f), new Vector2(bounds.center.x, bounds.yMin + 75f), new Vector2(bounds.width - 80f, 100f));
                SetRect((RectTransform)nextButton.transform, new Vector2(1f, .5f), new Vector2(-210f, 0f), new Vector2(300f, 84f));
                SetRect((RectTransform)backButton.transform, new Vector2(1f, .5f), new Vector2(-530f, 0f), new Vector2(230f, 78f));
                SetRect((RectTransform)exitButton.transform, new Vector2(0f, .5f), new Vector2(195f, 0f), new Vector2(220f, 74f));
                SetRect((RectTransform)inspectButton.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(280f, 56f));
                _screenWidth = Screen.width; _screenHeight = Screen.height;
                return;
            }
            Rect obstacle = default;
            if (_focus != null)
            {
                var corners = new Vector3[4];
                _focus.GetWorldCorners(corners);
                Canvas source = _focus.GetComponentInParent<Canvas>();
                Camera camera = source != null && source.renderMode != RenderMode.ScreenSpaceOverlay ? source.worldCamera : null;
                Vector2 lo = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                Vector2 hi = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                foreach (Vector3 corner in corners)
                {
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 p);
                    lo = Vector2.Min(lo, p); hi = Vector2.Max(hi, p);
                }
                obstacle = Rect.MinMaxRect(lo.x - 22f, lo.y - 22f, hi.x + 22f, hi.y + 22f);
            }
            float w = bounds.width - 80f;
            float h = 400f;
            var choices = new[]
            {
                new Rect(bounds.center.x - w * .5f, bounds.yMin + 65f, w, h),
                new Rect(bounds.center.x - w * .5f, bounds.yMax - h - 40f, w, h)
            };
            int best = 0;
            float bestOverlap = float.PositiveInfinity;
            for (int i = 0; i < choices.Length; i++)
            {
                Rect r = choices[i];
                float overlap = _focus == null ? 0f : Mathf.Max(0f, Mathf.Min(r.xMax, obstacle.xMax) - Mathf.Max(r.xMin, obstacle.xMin))
                    * Mathf.Max(0f, Mathf.Min(r.yMax, obstacle.yMax) - Mathf.Max(r.yMin, obstacle.yMin));
                if (overlap < bestOverlap) { bestOverlap = overlap; best = i; }
            }
            Rect selected = choices[best];
            SetRect(root, new Vector2(0.5f, 0.5f), selected.center, selected.size);
            SetRect(portrait, new Vector2(0f, .5f), new Vector2(195f, 42f), new Vector2(250f, 250f));
            Stretch(speechFrame, Vector2.zero, Vector2.zero);
            Stretch(bodyText.rectTransform, new Vector2(360f, 120f), new Vector2(-75f, -60f));
            SetRect(progressText.rectTransform, new Vector2(1f, 1f), new Vector2(-110f, -60f), new Vector2(150f, 36f));
            SetRect((RectTransform)nextButton.transform, new Vector2(1f, 0f), new Vector2(-210f, 95f), new Vector2(300f, 84f));
            SetRect((RectTransform)backButton.transform, new Vector2(1f, 0f), new Vector2(-530f, 95f), new Vector2(230f, 78f));
            SetRect((RectTransform)exitButton.transform, new Vector2(0f, 0f), new Vector2(195f, 95f), new Vector2(220f, 74f));
            if (inspectButton != null) SetRect((RectTransform)inspectButton.transform, new Vector2(0f, 0f), new Vector2(520f, 95f), new Vector2(280f, 56f));
            bodyText.fontSize = 32f;
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            _screenWidth = Screen.width; _screenHeight = Screen.height;
        }

        private static void SetRect(RectTransform r, Vector2 anchor, Vector2 position, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(.5f, .5f);
            r.anchoredPosition = position; r.sizeDelta = size;
        }

        private static void Stretch(RectTransform r, Vector2 min, Vector2 max)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = min; r.offsetMax = max;
        }
    }
}
