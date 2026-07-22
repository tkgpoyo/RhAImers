using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class NeonHoverButtonEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Target")]
    [SerializeField] private RectTransform _buttonRectTransform;
    [SerializeField] private Selectable _selectable;
    [SerializeField] private NeonHoverLinesGraphic _linesGraphic;

    [Header("Motion")]
    [SerializeField] private float _animationDuration = 0.16f;
    [SerializeField] private float _hoverLift = 7f;
    [SerializeField] private float _hoverScale = 0.025f;
    [SerializeField] private float _pressedLift = 2f;
    [SerializeField] private float _pressedScale = 0.01f;

    [Header("Line")]
    [SerializeField] private Color _leftColor = new Color(1f, 0.08f, 0.85f, 1f);
    [SerializeField] private Color _centerColor = new Color(0.75f, 0.18f, 1f, 1f);
    [SerializeField] private Color _rightColor = new Color(0.08f, 0.75f, 1f, 1f);
    [SerializeField] private float _lineThickness = 3f;
    [SerializeField] private float _lineInset = 2f;
    [SerializeField] private float _lineEndPadding = 18f;
    [SerializeField] private float _glowThickness = 12f;
    [SerializeField] private bool _autoScaleGlowWithLineThickness = true;
    [SerializeField] private float _glowToLineThicknessMultiplier = 3f;
    [SerializeField, Range(0f, 1f)] private float _lineAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float _glowAlpha = 0.28f;

    [Header("State")]
    [SerializeField] private bool _showWhenKeyboardSelected = true;

    private Vector2 _originalAnchoredPosition;
    private Vector3 _originalScale;
    private float _progress;
    private float _targetProgress;
    private float _velocity;
    private bool _isPointerOver;
    private bool _isSelected;
    private bool _isPressed;
    private bool _hasCapturedOriginalTransform;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        CaptureOriginalTransform();
        ApplyLineSettings();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureOriginalTransform();
        ApplyLineSettings();
        UpdateTargetProgress();
        ApplyVisualState(instant: true);
    }

    private void OnDisable()
    {
        _isPointerOver = false;
        _isSelected = false;
        _isPressed = false;
        _targetProgress = 0f;
        _progress = 0f;
        _velocity = 0f;

        if (_buttonRectTransform != null && _hasCapturedOriginalTransform)
        {
            _buttonRectTransform.anchoredPosition = _originalAnchoredPosition;
            _buttonRectTransform.localScale = _originalScale;
        }

        if (_linesGraphic != null)
        {
            _linesGraphic.Progress = 0f;
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
        ApplyLineSettings();
    }

    private void Update()
    {
        if (_buttonRectTransform == null || _linesGraphic == null)
        {
            ResolveReferences();
        }

        ApplyVisualState(instant: false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerOver = true;
        UpdateTargetProgress();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerOver = false;
        _isPressed = false;
        UpdateTargetProgress();
    }

    public void OnSelect(BaseEventData eventData)
    {
        _isSelected = true;
        UpdateTargetProgress();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isSelected = false;
        _isPressed = false;
        UpdateTargetProgress();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;
    }

    [ContextMenu("Recreate Hover Line Graphic")]
    private void RecreateHoverLineGraphic()
    {
        _linesGraphic = null;
        ResolveReferences(forceCreateLineGraphic: true);
        ApplyLineSettings();
    }

    [ContextMenu("Capture Current Transform As Rest Pose")]
    private void CaptureCurrentTransformAsRestPose()
    {
        _hasCapturedOriginalTransform = false;
        CaptureOriginalTransform();
    }

    private void ResolveReferences(bool forceCreateLineGraphic = false)
    {
        if (_buttonRectTransform == null)
        {
            _buttonRectTransform = transform as RectTransform;
        }

        if (_selectable == null)
        {
            _selectable = GetComponent<Selectable>();
        }

        if (_linesGraphic == null && !forceCreateLineGraphic)
        {
            _linesGraphic = GetComponentInChildren<NeonHoverLinesGraphic>(true);
        }

        if (_linesGraphic == null)
        {
            _linesGraphic = CreateLinesGraphic();
        }

        if (_linesGraphic != null)
        {
            _linesGraphic.raycastTarget = false;
            _linesGraphic.transform.SetAsLastSibling();
        }
    }

    private NeonHoverLinesGraphic CreateLinesGraphic()
    {
        var lineObject = new GameObject("HoverNeonLines", typeof(RectTransform), typeof(CanvasRenderer), typeof(NeonHoverLinesGraphic));
        lineObject.transform.SetParent(transform, false);

        RectTransform rectTransform = lineObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;

        return lineObject.GetComponent<NeonHoverLinesGraphic>();
    }

    private void CaptureOriginalTransform()
    {
        if (_buttonRectTransform == null || _hasCapturedOriginalTransform)
        {
            return;
        }

        _originalAnchoredPosition = _buttonRectTransform.anchoredPosition;
        _originalScale = _buttonRectTransform.localScale;
        _hasCapturedOriginalTransform = true;
    }

    private void ApplyLineSettings()
    {
        if (_linesGraphic == null)
        {
            return;
        }

        _linesGraphic.LeftColor = _leftColor;
        _linesGraphic.CenterColor = _centerColor;
        _linesGraphic.RightColor = _rightColor;
        _linesGraphic.LineThickness = _lineThickness;
        _linesGraphic.LineInset = _lineInset;
        _linesGraphic.LineEndPadding = _lineEndPadding;
        _linesGraphic.GlowThickness = _glowThickness;
        _linesGraphic.AutoScaleGlowWithLineThickness = _autoScaleGlowWithLineThickness;
        _linesGraphic.GlowToLineThicknessMultiplier = _glowToLineThicknessMultiplier;
        _linesGraphic.LineAlpha = _lineAlpha;
        _linesGraphic.GlowAlpha = _glowAlpha;
        _linesGraphic.SetVerticesDirty();
    }

    private void UpdateTargetProgress()
    {
        bool canShow = _selectable == null || _selectable.interactable;
        bool active = _isPointerOver || (_showWhenKeyboardSelected && _isSelected);
        _targetProgress = canShow && active ? 1f : 0f;
    }

    private void ApplyVisualState(bool instant)
    {
        if (_buttonRectTransform == null || _linesGraphic == null)
        {
            return;
        }

        UpdateTargetProgress();

        if (instant || _animationDuration <= 0f)
        {
            _progress = _targetProgress;
            _velocity = 0f;
        }
        else
        {
            _progress = Mathf.SmoothDamp(
                _progress,
                _targetProgress,
                ref _velocity,
                Mathf.Max(0.001f, _animationDuration),
                Mathf.Infinity,
                Time.unscaledDeltaTime);
        }

        float eased = EaseOutCubic(Mathf.Clamp01(_progress));
        float lift = _isPressed ? _pressedLift : _hoverLift;
        float scale = _isPressed ? _pressedScale : _hoverScale;

        _buttonRectTransform.anchoredPosition = _originalAnchoredPosition + Vector2.up * (lift * eased);
        _buttonRectTransform.localScale = _originalScale * (1f + scale * eased);

        _linesGraphic.Progress = eased;
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}

public sealed class NeonHoverLinesGraphic : MaskableGraphic
{
    [SerializeField, Range(0f, 1f)] private float _progress;
    [SerializeField] private Color _leftColor = new Color(1f, 0.08f, 0.85f, 1f);
    [SerializeField] private Color _centerColor = new Color(0.75f, 0.18f, 1f, 1f);
    [SerializeField] private Color _rightColor = new Color(0.08f, 0.75f, 1f, 1f);
    [SerializeField] private float _lineThickness = 3f;
    [SerializeField] private float _lineInset = 2f;
    [SerializeField] private float _lineEndPadding = 18f;
    [SerializeField] private float _glowThickness = 12f;
    [SerializeField] private bool _autoScaleGlowWithLineThickness = true;
    [SerializeField] private float _glowToLineThicknessMultiplier = 3f;
    [SerializeField, Range(0f, 1f)] private float _lineAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float _glowAlpha = 0.28f;

    public float Progress
    {
        get => _progress;
        set
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(_progress, clamped))
            {
                return;
            }

            _progress = clamped;
            SetVerticesDirty();
        }
    }

    public Color LeftColor
    {
        get => _leftColor;
        set
        {
            _leftColor = value;
            SetVerticesDirty();
        }
    }

    public Color CenterColor
    {
        get => _centerColor;
        set
        {
            _centerColor = value;
            SetVerticesDirty();
        }
    }

    public Color RightColor
    {
        get => _rightColor;
        set
        {
            _rightColor = value;
            SetVerticesDirty();
        }
    }

    public float LineThickness
    {
        get => _lineThickness;
        set
        {
            _lineThickness = value;
            SetVerticesDirty();
        }
    }

    public float LineInset
    {
        get => _lineInset;
        set
        {
            _lineInset = value;
            SetVerticesDirty();
        }
    }

    public float LineEndPadding
    {
        get => _lineEndPadding;
        set
        {
            _lineEndPadding = value;
            SetVerticesDirty();
        }
    }

    public float GlowThickness
    {
        get => _glowThickness;
        set
        {
            _glowThickness = value;
            SetVerticesDirty();
        }
    }

    public bool AutoScaleGlowWithLineThickness
    {
        get => _autoScaleGlowWithLineThickness;
        set
        {
            _autoScaleGlowWithLineThickness = value;
            SetVerticesDirty();
        }
    }

    public float GlowToLineThicknessMultiplier
    {
        get => _glowToLineThicknessMultiplier;
        set
        {
            _glowToLineThicknessMultiplier = value;
            SetVerticesDirty();
        }
    }

    public float LineAlpha
    {
        get => _lineAlpha;
        set
        {
            _lineAlpha = Mathf.Clamp01(value);
            SetVerticesDirty();
        }
    }

    public float GlowAlpha
    {
        get => _glowAlpha;
        set
        {
            _glowAlpha = Mathf.Clamp01(value);
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (_progress <= 0.001f)
        {
            return;
        }

        Rect rect = GetPixelAdjustedRect();
        float left = rect.xMin + _lineEndPadding;
        float right = rect.xMax - _lineEndPadding;
        float center = (left + right) * 0.5f;
        float halfWidth = Mathf.Max(0f, (right - left) * 0.5f * Mathf.Clamp01(_progress));

        if (halfWidth <= 0.5f)
        {
            return;
        }

        float topY = rect.yMax - _lineInset;
        float bottomY = rect.yMin + _lineInset;

        float effectiveLineThickness = Mathf.Max(0f, _lineThickness);
        float effectiveGlowThickness = GetEffectiveGlowThickness();

        if (effectiveGlowThickness > 0.001f && _glowAlpha > 0.001f)
        {
            AddHorizontalGradientLine(vertexHelper, center - halfWidth, center, topY, effectiveGlowThickness, _glowAlpha, _leftColor, _centerColor);
            AddHorizontalGradientLine(vertexHelper, center, center + halfWidth, topY, effectiveGlowThickness, _glowAlpha, _centerColor, _rightColor);
            AddHorizontalGradientLine(vertexHelper, center - halfWidth, center, bottomY, effectiveGlowThickness, _glowAlpha, _leftColor, _centerColor);
            AddHorizontalGradientLine(vertexHelper, center, center + halfWidth, bottomY, effectiveGlowThickness, _glowAlpha, _centerColor, _rightColor);
        }

        if (effectiveLineThickness > 0.001f && _lineAlpha > 0.001f)
        {
            AddHorizontalGradientLine(vertexHelper, center - halfWidth, center, topY, effectiveLineThickness, _lineAlpha, _leftColor, _centerColor);
            AddHorizontalGradientLine(vertexHelper, center, center + halfWidth, topY, effectiveLineThickness, _lineAlpha, _centerColor, _rightColor);
            AddHorizontalGradientLine(vertexHelper, center - halfWidth, center, bottomY, effectiveLineThickness, _lineAlpha, _leftColor, _centerColor);
            AddHorizontalGradientLine(vertexHelper, center, center + halfWidth, bottomY, effectiveLineThickness, _lineAlpha, _centerColor, _rightColor);
        }
    }

    private float GetEffectiveGlowThickness()
    {
        if (!_autoScaleGlowWithLineThickness)
        {
            return Mathf.Max(0f, _glowThickness);
        }

        float scaledGlowThickness = _lineThickness * _glowToLineThicknessMultiplier;
        float effectiveGlowThickness = _glowThickness < scaledGlowThickness ? _glowThickness : scaledGlowThickness;
        return Mathf.Max(0f, effectiveGlowThickness);
    }

    private static void AddHorizontalGradientLine(
        VertexHelper vertexHelper,
        float xMin,
        float xMax,
        float yCenter,
        float thickness,
        float alpha,
        Color leftColor,
        Color rightColor)
    {
        float yMin = yCenter - thickness * 0.5f;
        float yMax = yCenter + thickness * 0.5f;

        Color left = WithAlpha(leftColor, alpha);
        Color right = WithAlpha(rightColor, alpha);

        int startIndex = vertexHelper.currentVertCount;
        AddVertex(vertexHelper, new Vector2(xMin, yMin), left);
        AddVertex(vertexHelper, new Vector2(xMin, yMax), left);
        AddVertex(vertexHelper, new Vector2(xMax, yMax), right);
        AddVertex(vertexHelper, new Vector2(xMax, yMin), right);
        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }

    private static void AddVertex(VertexHelper vertexHelper, Vector2 position, Color color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        vertexHelper.AddVert(vertex);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(color.a * alpha);
        return color;
    }
}
