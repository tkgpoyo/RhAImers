using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UiButtonHoverScaleSoundEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    IPointerDownHandler,
    ISelectHandler,
    IDeselectHandler
{
    [Header("Target")]
    [SerializeField] private RectTransform _scaleTarget;
    [SerializeField] private Selectable _selectable;

    [Header("Scale")]
    [SerializeField] private bool _scaleOnPointerHover = true;
    [SerializeField] private bool _scaleOnKeyboardSelect = false;
    [SerializeField] private float _baseScaleMultiplier = 1f;
    [SerializeField] private float _hoverScaleMultiplier = 1.06f;
    [SerializeField] private float _animationDuration = 0.12f;
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Sound")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _hoverEnterClip;
    [SerializeField] private AudioClip _hoverExitClip;
    [SerializeField] private AudioClip _clickClip;
    [SerializeField] private float _enterVolume = 0.75f;
    [SerializeField] private float _exitVolume = 0.55f;
    [SerializeField] private float _clickVolume = 0.9f;
    [SerializeField] private bool _playSoundOnPointerHover = true;
    [SerializeField] private bool _playSoundOnKeyboardSelect = false;
    [SerializeField] private bool _playClickOnPointerDown = true;
    [SerializeField] private bool _playClickOnPointerClick = false;
    [SerializeField] private bool _playClickAsPersistentOneShot = true;
    [SerializeField] private float _minimumHoverSoundIntervalSeconds = 0.03f;
    [SerializeField] private float _minimumClickSoundIntervalSeconds = 0.03f;

    [Header("Behavior")]
    [SerializeField] private bool _ignoreWhenNotInteractable = true;
    [SerializeField] private bool _captureRestPoseOnEnable = true;
    [SerializeField] private bool _clearSelectionAfterPointerClick = true;

    private Vector3 _restScale = Vector3.one;
    private Vector3 _currentScaleVelocity;
    private bool _hasRestScale;
    private bool _isPointerInside;
    private bool _isSelected;
    private float _lastHoverSoundTime = -999f;
    private float _lastClickSoundTime = -999f;

    public float BaseScaleMultiplier
    {
        get => _baseScaleMultiplier;
        set => _baseScaleMultiplier = value;
    }

    private void Reset()
    {
        ResolveReferences();
        CaptureRestScale();
    }

    private void Awake()
    {
        ResolveReferences();
        CaptureRestScale();
        ConfigureAudioSource();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ConfigureAudioSource();

        _isPointerInside = false;
        _isSelected = false;
        _currentScaleVelocity = Vector3.zero;

        if (_captureRestPoseOnEnable || !_hasRestScale)
        {
            CaptureRestScale();
        }
    }

    private void OnDisable()
    {
        _isPointerInside = false;
        _isSelected = false;
        _currentScaleVelocity = Vector3.zero;

        if (_scaleTarget != null && _hasRestScale)
        {
            _scaleTarget.localScale = _restScale;
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (_scaleTarget == null)
        {
            ResolveReferences();
        }

        if (_scaleTarget == null || !_hasRestScale)
        {
            return;
        }

        bool shouldScale = ShouldShowHoverScale();
        float hoverMultiplier = shouldScale ? _hoverScaleMultiplier : 1f;
        Vector3 targetScale = _restScale * _baseScaleMultiplier * hoverMultiplier;

        if (_animationDuration <= 0f)
        {
            _scaleTarget.localScale = targetScale;
            _currentScaleVelocity = Vector3.zero;
            return;
        }

        float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _scaleTarget.localScale = Vector3.SmoothDamp(
            _scaleTarget.localScale,
            targetScale,
            ref _currentScaleVelocity,
            _animationDuration,
            Mathf.Infinity,
            deltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        bool wasInside = _isPointerInside;
        _isPointerInside = true;

        if (!wasInside && _playSoundOnPointerHover && CanReact())
        {
            PlayHoverOneShot(_hoverEnterClip, _enterVolume);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        bool wasInside = _isPointerInside;
        _isPointerInside = false;

        if (wasInside && _playSoundOnPointerHover && CanReact())
        {
            PlayHoverOneShot(_hoverExitClip, _exitVolume);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_playClickOnPointerDown || !CanReact())
        {
            return;
        }

        PlayClickOneShot();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_clearSelectionAfterPointerClick && EventSystem.current != null)
        {
            if (EventSystem.current.currentSelectedGameObject == gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            _isSelected = false;
        }

        if (!_playClickOnPointerClick || !CanReact())
        {
            return;
        }

        PlayClickOneShot();
    }

    public void OnSelect(BaseEventData eventData)
    {
        bool wasSelected = _isSelected;
        _isSelected = true;

        if (!wasSelected && _playSoundOnKeyboardSelect && CanReact())
        {
            PlayHoverOneShot(_hoverEnterClip, _enterVolume);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        bool wasSelected = _isSelected;
        _isSelected = false;

        if (wasSelected && _playSoundOnKeyboardSelect && CanReact())
        {
            PlayHoverOneShot(_hoverExitClip, _exitVolume);
        }
    }

    [ContextMenu("Capture Current Scale As Rest Scale")]
    private void CaptureCurrentScaleAsRestScale()
    {
        CaptureRestScale();
    }

    private void ResolveReferences()
    {
        if (_scaleTarget == null)
        {
            _scaleTarget = transform as RectTransform;
        }

        if (_selectable == null)
        {
            _selectable = GetComponent<Selectable>();
        }

        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        if (_audioSource == null)
        {
            _audioSource = GetComponentInParent<AudioSource>();
        }
    }

    private void ConfigureAudioSource()
    {
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
    }

    private void CaptureRestScale()
    {
        if (_scaleTarget == null)
        {
            return;
        }

        _restScale = _scaleTarget.localScale;
        _hasRestScale = true;
    }

    private bool ShouldShowHoverScale()
    {
        if (!CanReact())
        {
            return false;
        }

        bool pointerHover = _scaleOnPointerHover && _isPointerInside;
        bool keyboardSelect = _scaleOnKeyboardSelect && _isSelected;
        return pointerHover || keyboardSelect;
    }

    private bool CanReact()
    {
        if (_ignoreWhenNotInteractable && _selectable != null && !_selectable.interactable)
        {
            return false;
        }

        return true;
    }

    private void PlayHoverOneShot(AudioClip clip, float volume)
    {
        if (_audioSource == null || clip == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (now - _lastHoverSoundTime < _minimumHoverSoundIntervalSeconds)
        {
            return;
        }

        _lastHoverSoundTime = now;
        _audioSource.PlayOneShot(clip, Clamp01(volume));
    }

    private void PlayClickOneShot()
    {
        if (_clickClip == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (now - _lastClickSoundTime < _minimumClickSoundIntervalSeconds)
        {
            return;
        }

        _lastClickSoundTime = now;

        if (_playClickAsPersistentOneShot)
        {
            PlayPersistentOneShot(_clickClip, _clickVolume);
            return;
        }

        if (_audioSource == null)
        {
            return;
        }

        _audioSource.PlayOneShot(_clickClip, Clamp01(_clickVolume));
    }

    private static void PlayPersistentOneShot(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        var audioObject = new GameObject("UiButtonClickOneShotAudio");
        DontDestroyOnLoad(audioObject);

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = Clamp01(volume);
        source.clip = clip;
        source.Play();

        Destroy(audioObject, clip.length + 0.1f);
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
}
