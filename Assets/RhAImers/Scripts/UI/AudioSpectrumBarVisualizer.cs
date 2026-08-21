using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasRenderer))]
[AddComponentMenu("UI/Audio Spectrum Bar Visualizer")]
public sealed class AudioSpectrumBarVisualizer : MaskableGraphic
{
    private enum BarOrigin
    {
        Center,
        Bottom,
        Top
    }

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private bool _autoFindPlayingAudioSource = true;
    [SerializeField] private int _audioChannel = 0;
    [SerializeField] private int _spectrumSampleCount = 1024;
    [SerializeField] private FFTWindow _fftWindow = FFTWindow.BlackmanHarris;

    [Header("Bars")]
    [SerializeField] private int _barCount = 96;
    [SerializeField] private bool _fitBarsToRect = true;
    [SerializeField] private float _barWidth = 4f;
    [SerializeField] private float _barGap = 5f;
    [SerializeField] private float _sidePadding = 28f;
    [SerializeField] private float _baseBarHeight = 6f;
    [SerializeField] private float _barHeightRange = 230f;
    [SerializeField] private BarOrigin _barOrigin = BarOrigin.Center;
    [SerializeField] private float _originOffsetY = 0f;
    [SerializeField] private bool _mirrorAroundOrigin = true;
    [SerializeField] private bool _clipBarsToRect = true;

    [Header("Response")]
    [SerializeField] private float _sensitivity = 85f;
    [SerializeField] private float _lowFrequencyBoost = 1.15f;
    [SerializeField] private float _highFrequencyBoost = 1.45f;
    [SerializeField] private float _frequencyCurve = 2.15f;
    [SerializeField] private float _visualPower = 0.62f;
    [SerializeField] private float _riseSpeed = 18f;
    [SerializeField] private float _fallSpeed = 9f;
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Color")]
    [SerializeField] private Color _leftColor = new Color(1f, 0.06f, 0.82f, 0.72f);
    [SerializeField] private Color _centerColor = new Color(0.92f, 0.96f, 1f, 0.52f);
    [SerializeField] private Color _rightColor = new Color(0.05f, 0.76f, 1f, 0.72f);
    [SerializeField] private float _globalAlpha = 0.8f;

    [Header("Glow")]
    [SerializeField] private bool _drawGlow = true;
    [SerializeField] private int _glowPassCount = 2;
    [SerializeField] private float _glowWidth = 12f;
    [SerializeField] private float _glowHeight = 18f;
    [SerializeField] private float _glowAlpha = 0.18f;

    private float[] _spectrum;
    private float[] _barValues;
    private int _actualSampleCount;
    private int _actualBarCount;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        ResolveAudioSource();
        EnsureBuffers();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
        ResolveAudioSource();
        EnsureBuffers();
        SetVerticesDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        raycastTarget = false;
        EnsureBuffers();
        SetVerticesDirty();
    }
#endif

    private void Update()
    {
        EnsureBuffers();

        if (_audioSource == null && _autoFindPlayingAudioSource)
        {
            ResolveAudioSource();
        }

        UpdateSpectrumBars();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        EnsureBuffers();

        if (_barValues == null || _barValues.Length == 0)
        {
            return;
        }

        Rect rect = rectTransform.rect;
        float left = rect.x + _sidePadding;
        float right = rect.x + rect.width - _sidePadding;
        float usableWidth = right - left;

        if (usableWidth <= 0f)
        {
            return;
        }

        int count = _barValues.Length;
        float gap = _barGap;
        if (gap < 0f)
        {
            gap = 0f;
        }

        float width = _barWidth;
        float step = width + gap;

        if (_fitBarsToRect)
        {
            step = usableWidth / count;
            width = step - gap;

            if (width < 1f)
            {
                width = 1f;
            }
        }

        float originY = GetOriginY(rect);

        for (int i = 0; i < count; i++)
        {
            float t = count <= 1 ? 0.5f : i / (count - 1f);
            float centerX = _fitBarsToRect
                ? left + step * (i + 0.5f)
                : left + step * i + width * 0.5f;

            if (!_fitBarsToRect && centerX - width * 0.5f > right)
            {
                break;
            }

            float barHeight = _baseBarHeight + _barValues[i] * _barHeightRange;
            if (barHeight < 0f)
            {
                barHeight = 0f;
            }

            Color barColor = GetBarColor(t);
            AddBar(vh, rect, centerX, originY, width, barHeight, barColor, false);
        }
    }

    private void UpdateSpectrumBars()
    {
        float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        if (_barValues == null)
        {
            return;
        }

        if (_audioSource == null || !_audioSource.isActiveAndEnabled || _audioSource.clip == null)
        {
            FadeBarsToRest(deltaTime);
            return;
        }

        _audioSource.GetSpectrumData(_spectrum, _audioChannel, _fftWindow);

        int sampleLimit = _spectrum.Length;
        for (int i = 0; i < _barValues.Length; i++)
        {
            float start01 = i / (float)_barValues.Length;
            float end01 = (i + 1f) / _barValues.Length;
            int startIndex = SpectrumIndex(start01, sampleLimit);
            int endIndex = SpectrumIndex(end01, sampleLimit);

            if (endIndex <= startIndex)
            {
                endIndex = startIndex + 1;
            }

            if (endIndex > sampleLimit)
            {
                endIndex = sampleLimit;
            }

            float strongest = 0f;
            float total = 0f;
            int used = 0;

            for (int s = startIndex; s < endIndex; s++)
            {
                float value = _spectrum[s];
                total += value;
                used++;

                if (value > strongest)
                {
                    strongest = value;
                }
            }

            float average = used > 0 ? total / used : 0f;
            float level = average * 0.58f + strongest * 0.42f;
            float sideT = _barValues.Length <= 1 ? 0f : i / (_barValues.Length - 1f);
            float frequencyBoost = Mathf.Lerp(_lowFrequencyBoost, _highFrequencyBoost, sideT);
            float target = level * _sensitivity * frequencyBoost;

            if (target < 0f)
            {
                target = 0f;
            }

            if (target > 1f)
            {
                target = 1f;
            }

            if (_visualPower > 0f)
            {
                target = Mathf.Pow(target, _visualPower);
            }

            float speed = target > _barValues[i] ? _riseSpeed : _fallSpeed;
            if (speed <= 0f)
            {
                _barValues[i] = target;
                continue;
            }

            float blend = 1f - Mathf.Exp(-speed * deltaTime);
            _barValues[i] = Mathf.Lerp(_barValues[i], target, blend);
        }
    }

    private void FadeBarsToRest(float deltaTime)
    {
        if (_barValues == null)
        {
            return;
        }

        float speed = _fallSpeed;
        if (speed <= 0f)
        {
            for (int i = 0; i < _barValues.Length; i++)
            {
                _barValues[i] = 0f;
            }

            return;
        }

        float blend = 1f - Mathf.Exp(-speed * deltaTime);
        for (int i = 0; i < _barValues.Length; i++)
        {
            _barValues[i] = Mathf.Lerp(_barValues[i], 0f, blend);
        }
    }

    private int SpectrumIndex(float t, int sampleLimit)
    {
        if (sampleLimit <= 1)
        {
            return 0;
        }

        if (t < 0f)
        {
            t = 0f;
        }

        if (t > 1f)
        {
            t = 1f;
        }

        float curved = Mathf.Pow(t, _frequencyCurve);
        int index = Mathf.FloorToInt(curved * (sampleLimit - 1));

        if (index < 0)
        {
            return 0;
        }

        if (index >= sampleLimit)
        {
            return sampleLimit - 1;
        }

        return index;
    }

    private float GetOriginY(Rect rect)
    {
        switch (_barOrigin)
        {
            case BarOrigin.Bottom:
                return rect.y + _originOffsetY;
            case BarOrigin.Top:
                return rect.y + rect.height + _originOffsetY;
            default:
                return rect.y + rect.height * 0.5f + _originOffsetY;
        }
    }

    private Color GetBarColor(float t)
    {
        Color color;

        if (t < 0.5f)
        {
            color = Color.Lerp(_leftColor, _centerColor, t * 2f);
        }
        else
        {
            color = Color.Lerp(_centerColor, _rightColor, (t - 0.5f) * 2f);
        }

        color.a *= _globalAlpha;
        return color;
    }

    private void AddBar(
        VertexHelper vh,
        Rect rect,
        float centerX,
        float originY,
        float width,
        float height,
        Color color,
        bool glowOnly)
    {
        float halfWidth = width * 0.5f;
        float y0;
        float y1;

        if (_mirrorAroundOrigin)
        {
            float halfHeight = height * 0.5f;
            y0 = originY - halfHeight;
            y1 = originY + halfHeight;
        }
        else if (_barOrigin == BarOrigin.Top)
        {
            y0 = originY - height;
            y1 = originY;
        }
        else
        {
            y0 = originY;
            y1 = originY + height;
        }

        if (_clipBarsToRect)
        {
            float rectTop = rect.y + rect.height;

            if (y0 < rect.y)
            {
                y0 = rect.y;
            }

            if (y1 > rectTop)
            {
                y1 = rectTop;
            }
        }

        if (y1 <= y0)
        {
            return;
        }

        if (_drawGlow && !glowOnly)
        {
            int passCount = _glowPassCount;
            if (passCount < 0)
            {
                passCount = 0;
            }

            for (int pass = passCount; pass >= 1; pass--)
            {
                float passT = pass / (float)passCount;
                float glowWidth = width + _glowWidth * passT;
                float glowHeight = _glowHeight * passT;
                Color glowColor = color;
                glowColor.a *= _glowAlpha / pass;

                AddBarQuad(
                    vh,
                    centerX - glowWidth * 0.5f,
                    y0 - glowHeight * 0.5f,
                    centerX + glowWidth * 0.5f,
                    y1 + glowHeight * 0.5f,
                    glowColor);
            }
        }

        AddBarQuad(vh, centerX - halfWidth, y0, centerX + halfWidth, y1, color);
    }

    private static void AddBarQuad(VertexHelper vh, float x0, float y0, float x1, float y1, Color color)
    {
        int start = vh.currentVertCount;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = new Vector3(x0, y0, 0f);
        vh.AddVert(vertex);

        vertex.position = new Vector3(x0, y1, 0f);
        vh.AddVert(vertex);

        vertex.position = new Vector3(x1, y1, 0f);
        vh.AddVert(vertex);

        vertex.position = new Vector3(x1, y0, 0f);
        vh.AddVert(vertex);

        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }

    private void ResolveAudioSource()
    {
        if (_audioSource != null || !_autoFindPlayingAudioSource)
        {
            return;
        }

        AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];
            if (source != null && source.isActiveAndEnabled && source.isPlaying && source.clip != null && source.loop)
            {
                _audioSource = source;
                return;
            }
        }

        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource source = audioSources[i];
            if (source != null && source.isActiveAndEnabled && source.isPlaying && source.clip != null)
            {
                _audioSource = source;
                return;
            }
        }
    }

    private void EnsureBuffers()
    {
        int sampleCount = SanitizeSampleCount(_spectrumSampleCount);
        int barCount = _barCount;

        if (barCount < 1)
        {
            barCount = 1;
        }

        if (_spectrum == null || _actualSampleCount != sampleCount)
        {
            _spectrum = new float[sampleCount];
            _actualSampleCount = sampleCount;
        }

        if (_barValues == null || _actualBarCount != barCount)
        {
            _barValues = new float[barCount];
            _actualBarCount = barCount;
        }
    }

    private static int SanitizeSampleCount(int requested)
    {
        int value = requested;

        if (value < 64)
        {
            value = 64;
        }

        if (value > 8192)
        {
            value = 8192;
        }

        int power = 64;
        while (power < value && power < 8192)
        {
            power *= 2;
        }

        return power;
    }
}
