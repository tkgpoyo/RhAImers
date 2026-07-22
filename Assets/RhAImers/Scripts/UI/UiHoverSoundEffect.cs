using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UiHoverSoundEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler,
    IPointerDownHandler,
    IPointerClickHandler
{
    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _hoverEnterClip;
    [SerializeField] private AudioClip _hoverExitClip;
    [SerializeField] private AudioClip _clickClip;
    [SerializeField] private float _enterVolume = 0.8f;
    [SerializeField] private float _exitVolume = 0.65f;
    [SerializeField] private float _clickVolume = 0.9f;

    [Header("Behavior")]
    [SerializeField] private bool _playOnPointerHover = true;
    [SerializeField] private bool _playOnKeyboardSelect = false;
    [SerializeField] private bool _playClickOnPointerDown = true;
    [SerializeField] private bool _playClickOnPointerClick = false;
    [SerializeField] private bool _playClickAsPersistentOneShot = true;
    [SerializeField] private bool _ignoreWhenNotInteractable = true;
    [SerializeField] private float _minimumIntervalSeconds = 0.03f;

    private Selectable _selectable;
    private bool _isPointerInside;
    private bool _isSelected;
    private float _lastPlayedTime = -999f;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        ConfigureAudioSource();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ConfigureAudioSource();
        _isPointerInside = false;
        _isSelected = false;
    }

    private void OnDisable()
    {
        _isPointerInside = false;
        _isSelected = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_playOnPointerHover || _isPointerInside || !CanPlay())
        {
            _isPointerInside = true;
            return;
        }

        _isPointerInside = true;
        PlayOneShot(_hoverEnterClip, _enterVolume);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_playOnPointerHover || !_isPointerInside || !CanPlay())
        {
            _isPointerInside = false;
            return;
        }

        _isPointerInside = false;
        PlayOneShot(_hoverExitClip, _exitVolume);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!_playOnKeyboardSelect || _isSelected || !CanPlay())
        {
            _isSelected = true;
            return;
        }

        _isSelected = true;
        PlayOneShot(_hoverEnterClip, _enterVolume);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!_playOnKeyboardSelect || !_isSelected || !CanPlay())
        {
            _isSelected = false;
            return;
        }

        _isSelected = false;
        PlayOneShot(_hoverExitClip, _exitVolume);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_playClickOnPointerDown || !CanPlay())
        {
            return;
        }

        PlayClick();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_playClickOnPointerClick || !CanPlay())
        {
            return;
        }

        PlayClick();
    }

    private void ResolveReferences()
    {
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        if (_audioSource == null)
        {
            _audioSource = GetComponentInParent<AudioSource>();
        }

        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (_selectable == null)
        {
            _selectable = GetComponent<Selectable>();
        }
    }

    private void ConfigureAudioSource()
    {
        if (_audioSource == null)
        {
            return;
        }

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
    }

    private bool CanPlay()
    {
        if (_ignoreWhenNotInteractable && _selectable != null && !_selectable.interactable)
        {
            return false;
        }

        float now = Time.unscaledTime;
        if (now - _lastPlayedTime < _minimumIntervalSeconds)
        {
            return false;
        }

        return true;
    }

    private void PlayOneShot(AudioClip clip, float volume)
    {
        if (_audioSource == null || clip == null)
        {
            return;
        }

        _lastPlayedTime = Time.unscaledTime;
        _audioSource.PlayOneShot(clip, Clamp01(volume));
    }

    private void PlayClick()
    {
        if (_clickClip == null)
        {
            return;
        }

        _lastPlayedTime = Time.unscaledTime;

        if (_playClickAsPersistentOneShot)
        {
            PlayPersistentOneShot(_clickClip, _clickVolume);
            return;
        }

        PlayOneShot(_clickClip, _clickVolume);
    }

    private static void PlayPersistentOneShot(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        var audioObject = new GameObject("UiClickOneShotAudio");
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
