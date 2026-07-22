using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/BGM Scene Transition Button")]
public sealed class BgmSceneTransitionButton : Button
{
    [Header("BGM")]
    [SerializeField] private SceneBgmPlayer _sceneBgmPlayerToStop;
    [SerializeField] private AudioSource _bgmAudioSourceToStop;
    [SerializeField] private bool _autoFindSceneBgmPlayer = true;
    [SerializeField] private bool _stopBgmOnPress = true;

    [Header("Hold Before Fade")]
    [SerializeField] private float _holdBeforeFadeDuration = 0f;

    [Header("Fade")]
    [SerializeField] private Color _fadeColor = Color.black;
    [SerializeField] private float _fadeOutDuration = 0.45f;
    [SerializeField] private float _fadeInDuration = 0.45f;
    [SerializeField] private float _fadeInDelay = 0.05f;
    [SerializeField] private bool _ignoreTimeScale = true;

    [Header("Input")]
    [SerializeField] private bool _clearSelectionOnPress = true;
    [SerializeField] private bool _temporarilyDisableInteractable = true;

    private Coroutine _transitionCoroutine;
    private bool _isTransitioning;

    protected override void Awake()
    {
        base.Awake();
        ResolveBgmReference();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        StartTransition();
    }

    public override void OnSubmit(BaseEventData eventData)
    {
        StartTransition();
    }

    public void StartTransition()
    {
        if (_isTransitioning || !IsActive() || !IsInteractable())
        {
            return;
        }

        _isTransitioning = true;

        if (_temporarilyDisableInteractable)
        {
            interactable = false;
        }

        if (_clearSelectionOnPress && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        StopBgmIfNeeded();

        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }

        _transitionCoroutine = StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        if (_holdBeforeFadeDuration > 0f)
        {
            yield return Wait(_holdBeforeFadeDuration);
        }

        SceneFadeOverlay.Instance.PlayTransition(
            _fadeColor,
            _fadeOutDuration,
            _fadeInDuration,
            _fadeInDelay,
            _ignoreTimeScale,
            InvokeExistingButtonAction);

        _transitionCoroutine = null;
    }

    private void InvokeExistingButtonAction()
    {
        onClick.Invoke();

        if (this != null && isActiveAndEnabled)
        {
            StartCoroutine(RestoreStateAfterFrame());
        }
    }

    private IEnumerator RestoreStateAfterFrame()
    {
        yield return null;
        RestoreTransitionState();
    }

    private void RestoreTransitionState()
    {
        _isTransitioning = false;

        if (_temporarilyDisableInteractable)
        {
            interactable = true;
        }
    }

    private void StopBgmIfNeeded()
    {
        if (!_stopBgmOnPress)
        {
            return;
        }

        ResolveBgmReference();

        if (_sceneBgmPlayerToStop != null)
        {
            _sceneBgmPlayerToStop.Stop();
        }

        if (_bgmAudioSourceToStop != null)
        {
            _bgmAudioSourceToStop.Stop();
        }
    }

    private void ResolveBgmReference()
    {
        if (_sceneBgmPlayerToStop == null && _autoFindSceneBgmPlayer)
        {
            _sceneBgmPlayerToStop = FindFirstObjectByType<SceneBgmPlayer>();
        }
    }

    private IEnumerator Wait(float seconds)
    {
        float elapsed = 0f;

        while (elapsed < seconds)
        {
            elapsed += _ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }
}
