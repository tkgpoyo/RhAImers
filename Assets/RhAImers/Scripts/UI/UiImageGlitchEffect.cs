using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class UiImageGlitchEffect : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image _targetImage;
    [SerializeField] private UiImageGlitchOverlay _overlay;
    [SerializeField] private bool _createOverlayAutomatically = true;

    [Header("Timing")]
    [SerializeField] private bool _animateInEditMode = true;
    [SerializeField] private float _idleIntensity = 0.05f;
    [SerializeField] private float _burstIntensity = 0.85f;
    [SerializeField] private float _burstIntervalSeconds = 2.8f;
    [SerializeField] private float _burstIntervalVariationSeconds = 1.6f;
    [SerializeField] private float _burstDurationSeconds = 0.14f;
    [SerializeField] private float _burstDurationVariationSeconds = 0.08f;
    [SerializeField] private float _intensitySmoothing = 18f;

    [Header("Slice")]
    [SerializeField] private int _sliceCount = 9;
    [SerializeField] private float _sliceHeight = 14f;
    [SerializeField] private float _sliceHeightVariation = 28f;
    [SerializeField] private float _sliceOffset = 36f;
    [SerializeField] private float _sliceAlpha = 0.62f;

    [Header("RGB Shift")]
    [SerializeField] private float _rgbOffset = 7f;
    [SerializeField] private float _rgbAlpha = 0.58f;
    [SerializeField] private Color _leftChannelColor = new Color(1f, 0.06f, 0.72f, 1f);
    [SerializeField] private Color _rightChannelColor = new Color(0.03f, 0.74f, 1f, 1f);

    [Header("Shape")]
    [SerializeField] private bool _preserveTargetAspect = true;
    [SerializeField] private bool _copyTargetColor = true;

    private float _nextBurstTime;
    private float _burstEndTime;
    private float _currentIntensity;
    private int _frameSeed;
    private System.Random _random;
    private double _lastEditorTime;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        ScheduleNextBurst(GetCurrentTime());
        ApplyOverlaySettings();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ScheduleNextBurst(GetCurrentTime());
        ApplyOverlaySettings();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall -= ApplyValidationDelayedInEditor;
            UnityEditor.EditorApplication.delayCall += ApplyValidationDelayedInEditor;
            return;
        }
#endif

        ResolveReferences();
        ApplyOverlaySettings();
    }

#if UNITY_EDITOR
    private void ApplyValidationDelayedInEditor()
    {
        UnityEditor.EditorApplication.delayCall -= ApplyValidationDelayedInEditor;

        if (this == null)
        {
            return;
        }

        ResolveReferences();
        ApplyOverlaySettings();

        if (_overlay != null)
        {
            _overlay.SetVerticesDirty();
            _overlay.SetMaterialDirty();
        }
    }
#endif

    private void Update()
    {
        if (!Application.isPlaying && !_animateInEditMode)
        {
            return;
        }

        ResolveReferences();

        if (_overlay == null)
        {
            return;
        }

        float now = GetCurrentTime();
        float deltaTime = GetDeltaTime(now);

        if (now >= _nextBurstTime)
        {
            float duration = _burstDurationSeconds + RandomRange(-_burstDurationVariationSeconds, _burstDurationVariationSeconds);
            if (duration < 0f)
            {
                duration = 0f;
            }

            _burstEndTime = now + duration;
            ScheduleNextBurst(now);
            _frameSeed += 173;
        }

        float targetIntensity = _idleIntensity;
        if (now <= _burstEndTime)
        {
            float burstProgress = SafeDivide(_burstEndTime - now, _burstEndTime - (_burstEndTime - _burstDurationSeconds));
            burstProgress = Clamp01(burstProgress);
            targetIntensity = _burstIntensity * EaseOutCubic(burstProgress);
        }

        if (_intensitySmoothing <= 0f || deltaTime <= 0f)
        {
            _currentIntensity = targetIntensity;
        }
        else
        {
            float t = 1f - Mathf.Exp(-_intensitySmoothing * deltaTime);
            _currentIntensity = Mathf.Lerp(_currentIntensity, targetIntensity, Clamp01(t));
        }

        _frameSeed++;
        _overlay.GlitchSeed = _frameSeed;
        _overlay.Intensity = Clamp01(_currentIntensity);
        ApplyOverlaySettings();
        _overlay.SetVerticesDirty();
    }

    [ContextMenu("Recreate Glitch Overlay")]
    private void RecreateOverlay()
    {
        if (_overlay != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_overlay.gameObject);
            }
            else
            {
                DestroyImmediate(_overlay.gameObject);
            }
        }

        _overlay = null;
        ResolveReferences(forceCreateOverlay: true);
        ApplyOverlaySettings();
    }

    private void ResolveReferences(bool forceCreateOverlay = false)
    {
        if (_targetImage == null)
        {
            _targetImage = GetComponent<Image>();
        }

        if (_overlay == null && !forceCreateOverlay)
        {
            _overlay = GetComponentInChildren<UiImageGlitchOverlay>(true);
        }

        if (_overlay == null && _createOverlayAutomatically)
        {
            _overlay = CreateOverlay();
        }
    }

    private UiImageGlitchOverlay CreateOverlay()
    {
        GameObject overlayObject = new GameObject("GlitchOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(UiImageGlitchOverlay));
        overlayObject.transform.SetParent(transform, false);

        RectTransform rectTransform = overlayObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;

        UiImageGlitchOverlay createdOverlay = overlayObject.GetComponent<UiImageGlitchOverlay>();
        createdOverlay.raycastTarget = false;
        createdOverlay.transform.SetAsLastSibling();
        return createdOverlay;
    }

    private void ApplyOverlaySettings()
    {
        if (_overlay == null)
        {
            return;
        }

        if (_targetImage != null)
        {
            _overlay.SourceSprite = _targetImage.sprite;
            _overlay.TargetColor = _copyTargetColor ? _targetImage.color : Color.white;
            _overlay.PreserveAspect = _preserveTargetAspect && _targetImage.preserveAspect;
            _overlay.material = _targetImage.material;
        }

        _overlay.SliceCount = _sliceCount;
        _overlay.SliceHeight = _sliceHeight;
        _overlay.SliceHeightVariation = _sliceHeightVariation;
        _overlay.SliceOffset = _sliceOffset;
        _overlay.SliceAlpha = _sliceAlpha;
        _overlay.RgbOffset = _rgbOffset;
        _overlay.RgbAlpha = _rgbAlpha;
        _overlay.LeftChannelColor = _leftChannelColor;
        _overlay.RightChannelColor = _rightChannelColor;
    }

    private void ScheduleNextBurst(float now)
    {
        float interval = _burstIntervalSeconds + RandomRange(-_burstIntervalVariationSeconds, _burstIntervalVariationSeconds);
        if (interval < 0f)
        {
            interval = 0f;
        }

        _nextBurstTime = now + interval;
    }

    private float RandomRange(float low, float high)
    {
        if (_random == null)
        {
            _random = new System.Random(5279 + GetInstanceID());
        }

        if (high < low)
        {
            float swap = low;
            low = high;
            high = swap;
        }

        return low + (float)_random.NextDouble() * (high - low);
    }

    private float GetCurrentTime()
    {
        if (Application.isPlaying)
        {
            return Time.unscaledTime;
        }

#if UNITY_EDITOR
        return (float)UnityEditor.EditorApplication.timeSinceStartup;
#else
        return Time.unscaledTime;
#endif
    }

    private float GetDeltaTime(float now)
    {
        if (Application.isPlaying)
        {
            return Time.unscaledDeltaTime;
        }

        if (_lastEditorTime <= 0d)
        {
            _lastEditorTime = now;
            return 0f;
        }

        float delta = now - (float)_lastEditorTime;
        _lastEditorTime = now;

        if (delta < 0f)
        {
            return 0f;
        }

        if (delta > 0.1f)
        {
            return 0.1f;
        }

        return delta;
    }

    private static float SafeDivide(float value, float divisor)
    {
        if (Mathf.Abs(divisor) <= 0.0001f)
        {
            return 0f;
        }

        return value / divisor;
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

    private static float EaseOutCubic(float value)
    {
        value = Clamp01(value);
        float inverse = 1f - value;
        return 1f - inverse * inverse * inverse;
    }
}

public sealed class UiImageGlitchOverlay : MaskableGraphic
{
    [SerializeField] private Sprite _sourceSprite;
    [SerializeField] private Color _targetColor = Color.white;
    [SerializeField] private bool _preserveAspect = true;
    [SerializeField] private float _intensity;
    [SerializeField] private int _glitchSeed;
    [SerializeField] private int _sliceCount = 9;
    [SerializeField] private float _sliceHeight = 14f;
    [SerializeField] private float _sliceHeightVariation = 28f;
    [SerializeField] private float _sliceOffset = 36f;
    [SerializeField] private float _sliceAlpha = 0.62f;
    [SerializeField] private float _rgbOffset = 7f;
    [SerializeField] private float _rgbAlpha = 0.58f;
    [SerializeField] private Color _leftChannelColor = new Color(1f, 0.06f, 0.72f, 1f);
    [SerializeField] private Color _rightChannelColor = new Color(0.03f, 0.74f, 1f, 1f);

    public override Texture mainTexture
    {
        get
        {
            if (_sourceSprite != null && _sourceSprite.texture != null)
            {
                return _sourceSprite.texture;
            }

            return s_WhiteTexture;
        }
    }

    public Sprite SourceSprite
    {
        get => _sourceSprite;
        set
        {
            _sourceSprite = value;
            SetVerticesDirty();
            SetMaterialDirty();
        }
    }

    public Color TargetColor
    {
        get => _targetColor;
        set
        {
            _targetColor = value;
            SetVerticesDirty();
        }
    }

    public bool PreserveAspect
    {
        get => _preserveAspect;
        set
        {
            _preserveAspect = value;
            SetVerticesDirty();
        }
    }

    public float Intensity
    {
        get => _intensity;
        set
        {
            _intensity = Clamp01(value);
            SetVerticesDirty();
        }
    }

    public int GlitchSeed
    {
        get => _glitchSeed;
        set
        {
            _glitchSeed = value;
            SetVerticesDirty();
        }
    }

    public int SliceCount
    {
        get => _sliceCount;
        set
        {
            _sliceCount = value;
            SetVerticesDirty();
        }
    }

    public float SliceHeight
    {
        get => _sliceHeight;
        set
        {
            _sliceHeight = value;
            SetVerticesDirty();
        }
    }

    public float SliceHeightVariation
    {
        get => _sliceHeightVariation;
        set
        {
            _sliceHeightVariation = value;
            SetVerticesDirty();
        }
    }

    public float SliceOffset
    {
        get => _sliceOffset;
        set
        {
            _sliceOffset = value;
            SetVerticesDirty();
        }
    }

    public float SliceAlpha
    {
        get => _sliceAlpha;
        set
        {
            _sliceAlpha = value;
            SetVerticesDirty();
        }
    }

    public float RgbOffset
    {
        get => _rgbOffset;
        set
        {
            _rgbOffset = value;
            SetVerticesDirty();
        }
    }

    public float RgbAlpha
    {
        get => _rgbAlpha;
        set
        {
            _rgbAlpha = value;
            SetVerticesDirty();
        }
    }

    public Color LeftChannelColor
    {
        get => _leftChannelColor;
        set
        {
            _leftChannelColor = value;
            SetVerticesDirty();
        }
    }

    public Color RightChannelColor
    {
        get => _rightChannelColor;
        set
        {
            _rightChannelColor = value;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (_intensity <= 0.001f)
        {
            return;
        }

        Rect drawRect = GetDrawRect();
        if (drawRect.width <= 0f || drawRect.height <= 0f)
        {
            return;
        }

        Vector4 uv = GetOuterUv();
        int count = _sliceCount;
        if (count < 0)
        {
            count = 0;
        }

        for (int i = 0; i < count; i++)
        {
            float randomY = Hash01(_glitchSeed, i * 19 + 3);
            float randomHeight = Hash01(_glitchSeed, i * 31 + 11);
            float randomOffset = Hash01(_glitchSeed, i * 47 + 17) * 2f - 1f;
            float randomAlpha = 0.45f + Hash01(_glitchSeed, i * 59 + 23) * 0.55f;

            float height = _sliceHeight + _sliceHeightVariation * randomHeight;
            if (height < 0f)
            {
                height = 0f;
            }

            float yCenter = drawRect.yMin + drawRect.height * randomY;
            float yMin = yCenter - height * 0.5f;
            float yMax = yCenter + height * 0.5f;

            if (yMax < drawRect.yMin || yMin > drawRect.yMax)
            {
                continue;
            }

            if (yMin < drawRect.yMin)
            {
                yMin = drawRect.yMin;
            }

            if (yMax > drawRect.yMax)
            {
                yMax = drawRect.yMax;
            }

            float offset = randomOffset * _sliceOffset * _intensity;
            float rgbOffset = _rgbOffset * _intensity;
            float alpha = Clamp01(_sliceAlpha * _intensity * randomAlpha);
            float rgbAlpha = Clamp01(_rgbAlpha * _intensity * randomAlpha);

            AddSlice(vertexHelper, drawRect, uv, yMin, yMax, offset, WithAlpha(_targetColor, alpha));
            AddSlice(vertexHelper, drawRect, uv, yMin, yMax, offset - rgbOffset, WithAlpha(_leftChannelColor, rgbAlpha));
            AddSlice(vertexHelper, drawRect, uv, yMin, yMax, offset + rgbOffset, WithAlpha(_rightChannelColor, rgbAlpha));
        }
    }

    private Rect GetDrawRect()
    {
        Rect rect = GetPixelAdjustedRect();

        if (_sourceSprite == null || !_preserveAspect || rect.width <= 0f || rect.height <= 0f)
        {
            return rect;
        }

        float spriteWidth = _sourceSprite.rect.width;
        float spriteHeight = _sourceSprite.rect.height;
        if (spriteWidth <= 0f || spriteHeight <= 0f)
        {
            return rect;
        }

        float spriteRatio = spriteWidth / spriteHeight;
        float rectRatio = rect.width / rect.height;

        if (rectRatio > spriteRatio)
        {
            float width = rect.height * spriteRatio;
            float x = rect.x + (rect.width - width) * 0.5f;
            return new Rect(x, rect.y, width, rect.height);
        }

        float height = rect.width / spriteRatio;
        float y = rect.y + (rect.height - height) * 0.5f;
        return new Rect(rect.x, y, rect.width, height);
    }

    private Vector4 GetOuterUv()
    {
        if (_sourceSprite == null)
        {
            return new Vector4(0f, 0f, 1f, 1f);
        }

        return UnityEngine.Sprites.DataUtility.GetOuterUV(_sourceSprite);
    }

    private static void AddSlice(
        VertexHelper vertexHelper,
        Rect drawRect,
        Vector4 outerUv,
        float yMin,
        float yMax,
        float xOffset,
        Color color)
    {
        if (color.a <= 0f || yMax <= yMin)
        {
            return;
        }

        float y01Min = SafeDivide(yMin - drawRect.yMin, drawRect.height);
        float y01Max = SafeDivide(yMax - drawRect.yMin, drawRect.height);

        float uvYMin = Mathf.Lerp(outerUv.y, outerUv.w, y01Min);
        float uvYMax = Mathf.Lerp(outerUv.y, outerUv.w, y01Max);

        float xMin = drawRect.xMin + xOffset;
        float xMax = drawRect.xMax + xOffset;

        int startIndex = vertexHelper.currentVertCount;
        AddVertex(vertexHelper, new Vector2(xMin, yMin), new Vector2(outerUv.x, uvYMin), color);
        AddVertex(vertexHelper, new Vector2(xMin, yMax), new Vector2(outerUv.x, uvYMax), color);
        AddVertex(vertexHelper, new Vector2(xMax, yMax), new Vector2(outerUv.z, uvYMax), color);
        AddVertex(vertexHelper, new Vector2(xMax, yMin), new Vector2(outerUv.z, uvYMin), color);

        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }

    private static void AddVertex(VertexHelper vertexHelper, Vector2 position, Vector2 uv, Color color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.uv0 = uv;
        vertex.color = color;
        vertexHelper.AddVert(vertex);
    }

    private static float Hash01(int seed, int index)
    {
        uint x = (uint)(seed + index * 374761393);
        x = (x ^ (x >> 13)) * 1274126177u;
        x ^= x >> 16;
        return (x & 0x00FFFFFF) / 16777215f;
    }

    private static float SafeDivide(float value, float divisor)
    {
        if (Mathf.Abs(divisor) <= 0.0001f)
        {
            return 0f;
        }

        return value / divisor;
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

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a *= Clamp01(alpha);
        return color;
    }
}
