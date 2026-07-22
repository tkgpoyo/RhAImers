using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SceneBgmPlayer : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _bgmClip;
    [SerializeField] private bool _playOnEnable = true;
    [SerializeField] private bool _loop = true;
    [SerializeField] private float _volume = 0.45f;

    [Header("Fade")]
    [SerializeField] private bool _fadeIn = true;
    [SerializeField] private float _fadeInDuration = 0.8f;

    private Coroutine _fadeCoroutine;

    private void Reset()
    {
        ResolveAudioSource();
    }

    private void Awake()
    {
        ResolveAudioSource();
        ConfigureAudioSource();
    }

    private void OnEnable()
    {
        ResolveAudioSource();
        ConfigureAudioSource();

        if (_playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        StopFadeCoroutine();
    }

    public void Play()
    {
        if (_audioSource == null || _bgmClip == null)
        {
            return;
        }

        ConfigureAudioSource();

        if (_audioSource.clip != _bgmClip)
        {
            _audioSource.clip = _bgmClip;
        }

        if (!_audioSource.isPlaying)
        {
            _audioSource.volume = _fadeIn ? 0f : Clamp01(_volume);
            _audioSource.Play();
        }

        if (_fadeIn)
        {
            FadeTo(_volume, _fadeInDuration);
        }
        else
        {
            _audioSource.volume = Clamp01(_volume);
        }
    }

    public void Stop()
    {
        StopFadeCoroutine();

        if (_audioSource != null)
        {
            _audioSource.Stop();
        }
    }

    private void ResolveAudioSource()
    {
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void ConfigureAudioSource()
    {
        if (_audioSource == null)
        {
            return;
        }

        _audioSource.playOnAwake = false;
        _audioSource.loop = _loop;
        _audioSource.spatialBlend = 0f;
    }

    private void FadeTo(float targetVolume, float duration)
    {
        StopFadeCoroutine();
        _fadeCoroutine = StartCoroutine(FadeRoutine(Clamp01(targetVolume), duration));
    }

    private IEnumerator FadeRoutine(float targetVolume, float duration)
    {
        if (_audioSource == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            _audioSource.volume = targetVolume;
            _fadeCoroutine = null;
            yield break;
        }

        float startVolume = _audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Clamp01(elapsed / duration);
            float eased = EaseInOutCubic(t);
            _audioSource.volume = Mathf.Lerp(startVolume, targetVolume, eased);
            yield return null;
        }

        _audioSource.volume = targetVolume;
        _fadeCoroutine = null;
    }

    private void StopFadeCoroutine()
    {
        if (_fadeCoroutine == null)
        {
            return;
        }

        StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = null;
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
