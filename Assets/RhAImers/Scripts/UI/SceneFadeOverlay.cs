using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class SceneFadeOverlay : MonoBehaviour
{
    private static SceneFadeOverlay _instance;

    [SerializeField] private Canvas _canvas;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _fadeImage;

    private Coroutine _transitionCoroutine;
    private static bool _isTransitionActive;

    public static bool IsTransitionActiveOrVisible
    {
        get
        {
            if (_isTransitionActive)
            {
                return true;
            }

            return _instance != null
                && _instance._canvasGroup != null
                && _instance._canvasGroup.alpha > 0.001f;
        }
    }

    public static SceneFadeOverlay Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SceneFadeOverlay>();
            }

            if (_instance == null)
            {
                _instance = CreateInstance();
            }

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureReferences();
        SetAlpha(0f);
        SetBlocksRaycasts(false);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _isTransitionActive = false;
            _instance = null;
        }
    }

    public void PlayTransition(
        Color fadeColor,
        float fadeOutDuration,
        float fadeInDuration,
        float fadeInDelay,
        bool ignoreTimeScale,
        Action onFadeOutComplete)
    {
        EnsureReferences();

        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }

        _isTransitionActive = true;
        _transitionCoroutine = StartCoroutine(TransitionRoutine(
            fadeColor,
            fadeOutDuration,
            fadeInDuration,
            fadeInDelay,
            ignoreTimeScale,
            onFadeOutComplete));
    }

    private IEnumerator TransitionRoutine(
        Color fadeColor,
        float fadeOutDuration,
        float fadeInDuration,
        float fadeInDelay,
        bool ignoreTimeScale,
        Action onFadeOutComplete)
    {
        EnsureReferences();

        if (_fadeImage != null)
        {
            _fadeImage.color = fadeColor;
        }

        SetBlocksRaycasts(true);
        yield return FadeAlpha(0f, 1f, fadeOutDuration, ignoreTimeScale);

        onFadeOutComplete?.Invoke();

        yield return null;

        if (fadeInDelay > 0f)
        {
            yield return Wait(fadeInDelay, ignoreTimeScale);
        }

        yield return FadeAlpha(1f, 0f, fadeInDuration, ignoreTimeScale);
        SetBlocksRaycasts(false);
        _isTransitionActive = false;
        _transitionCoroutine = null;
    }

    private IEnumerator FadeAlpha(float from, float to, float duration, bool ignoreTimeScale)
    {
        if (duration <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Clamp01(elapsed / duration);
            float eased = EaseInOutCubic(t);
            SetAlpha(Mathf.Lerp(from, to, eased));
            yield return null;
        }

        SetAlpha(to);
    }

    private IEnumerator Wait(float seconds, bool ignoreTimeScale)
    {
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            elapsed += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private void EnsureReferences()
    {
        if (_canvas == null)
        {
            _canvas = GetComponentInChildren<Canvas>(true);
        }

        if (_canvas == null)
        {
            GameObject canvasObject = new GameObject("SceneFadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
        }

        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 32767;

        CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = _canvas.GetComponent<CanvasGroup>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = _canvas.gameObject.AddComponent<CanvasGroup>();
        }

        if (_fadeImage == null)
        {
            _fadeImage = _canvas.GetComponentInChildren<Image>(true);
        }

        if (_fadeImage == null)
        {
            GameObject imageObject = new GameObject("FadeImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(_canvas.transform, false);
            _fadeImage = imageObject.GetComponent<Image>();
        }

        RectTransform imageRectTransform = _fadeImage.transform as RectTransform;
        if (imageRectTransform != null)
        {
            imageRectTransform.anchorMin = Vector2.zero;
            imageRectTransform.anchorMax = Vector2.one;
            imageRectTransform.pivot = new Vector2(0.5f, 0.5f);
            imageRectTransform.anchoredPosition = Vector2.zero;
            imageRectTransform.sizeDelta = Vector2.zero;
            imageRectTransform.localScale = Vector3.one;
            imageRectTransform.localRotation = Quaternion.identity;
        }

        _fadeImage.raycastTarget = true;
    }

    private void SetAlpha(float alpha)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = Clamp01(alpha);
        }
    }

    private void SetBlocksRaycasts(bool blocks)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = blocks;
            _canvasGroup.interactable = blocks;
        }
    }

    private static SceneFadeOverlay CreateInstance()
    {
        GameObject overlayObject = new GameObject("SceneFadeOverlay", typeof(SceneFadeOverlay));
        return overlayObject.GetComponent<SceneFadeOverlay>();
    }

    private static float Clamp01(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        if (value > 1f)
        {
            return 1f;
        }

        return value;
    }

    private static float EaseInOutCubic(float t)
    {
        t = Clamp01(t);

        if (t < 0.5f)
        {
            return 4f * t * t * t;
        }

        float f = -2f * t + 2f;
        return 1f - (f * f * f) * 0.5f;
    }
}
