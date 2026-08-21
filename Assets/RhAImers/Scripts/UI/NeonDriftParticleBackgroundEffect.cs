using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class NeonDriftParticleBackgroundEffect : MaskableGraphic
{
    [Header("Layout")]
    [SerializeField] private bool _autoStretchToParent = true;

    [Header("Particles")]
    [SerializeField] private int _particleCount = 120;
    [SerializeField] private float _particleSize = 4.2f;
    [SerializeField] private float _sizeVariation = 2.8f;
    [SerializeField] private float _glowSizeMultiplier = 5.2f;
    [SerializeField] private float _coreAlpha = 0.9f;
    [SerializeField] private float _glowAlpha = 0.26f;

    [Header("Motion")]
    [SerializeField] private float _lifetimeSeconds = 4.8f;
    [SerializeField] private float _lifetimeVariationSeconds = 2.2f;
    [SerializeField] private float _upwardTravelDistance = 190f;
    [SerializeField] private float _centerTravelDistance = 130f;
    [SerializeField] private float _horizontalDrift = 28f;
    [SerializeField] private float _spawnPadding = 24f;

    [Header("Color")]
    [SerializeField] private Color _leftParticleColor = new Color(1f, 0.05f, 0.78f, 1f);
    [SerializeField] private Color _rightParticleColor = new Color(0.05f, 0.72f, 1f, 1f);
    [SerializeField] private Color _centerBlendColor = new Color(0.65f, 0.18f, 1f, 1f);
    [SerializeField] private float _centerBlendWidth = 180f;

    [Header("Timing")]
    [SerializeField] private bool _animateInEditMode = true;
    [SerializeField] private int _randomSeed = 417;

    private readonly List<Particle> _particles = new();
    private System.Random _random;
    private Rect _lastRect;
    private double _lastEditorTime;

    private struct Particle
    {
        public Vector2 Start;
        public Vector2 End;
        public float Age;
        public float Lifetime;
        public float Size;
        public Color Color;
        public float Phase;
    }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        canvasRenderer.cullTransparentMesh = false;
        ApplyAutoStretch();
        InitializeParticles();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
        canvasRenderer.cullTransparentMesh = false;
        ApplyAutoStretch();
        InitializeParticles();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        raycastTarget = false;
        ApplyAutoStretch();
        InitializeParticles();
        SetVerticesDirty();
    }
#endif

    private void Update()
    {
        if (!Application.isPlaying && !_animateInEditMode)
        {
            return;
        }

        ApplyAutoStretch();

        Rect rect = GetUsableRect();
        if (!ApproximatelySameRect(rect, _lastRect))
        {
            InitializeParticles();
            _lastRect = rect;
        }

        float deltaTime = GetDeltaTime();
        if (deltaTime <= 0f)
        {
            return;
        }

        AdvanceParticles(deltaTime);
        SetVerticesDirty();
    }

    [ContextMenu("Reset Particles")]
    public void ResetParticles()
    {
        InitializeParticles();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetUsableRect();
        if (rect.width <= 0f || rect.height <= 0f || _particles.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _particles.Count; i++)
        {
            Particle particle = _particles[i];
            float progress = SafeDivide(particle.Age, particle.Lifetime);
            progress = Clamp01(progress);

            Vector2 position = Vector2.Lerp(particle.Start, particle.End, EaseOutCubic(progress));
            position += GetSoftDriftOffset(particle, progress);

            float fade = Mathf.Sin(progress * Mathf.PI);
            fade *= 1f - SmoothStep(0.72f, 1f, progress);
            fade = Clamp01(fade);

            float coreSize = particle.Size * (0.75f + 0.25f * fade);
            float glowSize = particle.Size * _glowSizeMultiplier;

            AddParticleQuad(vertexHelper, position, glowSize, WithAlpha(particle.Color, _glowAlpha * fade));
            AddParticleQuad(vertexHelper, position, coreSize, WithAlpha(particle.Color, _coreAlpha * fade));
        }
    }

    private void InitializeParticles()
    {
        Rect rect = GetUsableRect();
        if (rect.width <= 0f || rect.height <= 0f)
        {
            rect = new Rect(-960f, -540f, 1920f, 1080f);
        }

        _lastRect = rect;
        _random = new System.Random(_randomSeed);
        _particles.Clear();

        int count = _particleCount;
        if (count < 0)
        {
            count = 0;
        }

        for (int i = 0; i < count; i++)
        {
            Particle particle = CreateParticle(rect);
            particle.Age = RandomRange(0f, particle.Lifetime);
            _particles.Add(particle);
        }
    }

    private void AdvanceParticles(float deltaTime)
    {
        Rect rect = GetUsableRect();

        for (int i = 0; i < _particles.Count; i++)
        {
            Particle particle = _particles[i];
            particle.Age += deltaTime;

            if (particle.Age >= particle.Lifetime)
            {
                particle = CreateParticle(rect);
            }

            _particles[i] = particle;
        }
    }

    private void ApplyAutoStretch()
    {
        if (!_autoStretchToParent || transform.parent == null)
        {
            return;
        }

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }

    private Rect GetUsableRect()
    {
        Rect rect = GetPixelAdjustedRect();
        if (rect.width > 1f && rect.height > 1f)
        {
            return rect;
        }

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null && rectTransform.rect.width > 1f && rectTransform.rect.height > 1f)
        {
            return rectTransform.rect;
        }

        if (transform.parent is RectTransform parentRectTransform &&
            parentRectTransform.rect.width > 1f &&
            parentRectTransform.rect.height > 1f)
        {
            Vector2 size = parentRectTransform.rect.size;
            return new Rect(-size.x * 0.5f, -size.y * 0.5f, size.x, size.y);
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.pixelRect.width > 1f && canvas.pixelRect.height > 1f)
        {
            Vector2 size = canvas.pixelRect.size;
            return new Rect(-size.x * 0.5f, -size.y * 0.5f, size.x, size.y);
        }

        return new Rect(-960f, -540f, 1920f, 1080f);
    }

    private Particle CreateParticle(Rect rect)
    {
        Vector2 start = GetRandomStartPosition(rect);
        Vector2 center = rect.center;
        Vector2 toCenter = center - start;

        if (toCenter.sqrMagnitude > 0.001f)
        {
            toCenter.Normalize();
        }
        else
        {
            toCenter = Vector2.up;
        }

        float sideDrift = RandomRange(-_horizontalDrift, _horizontalDrift);
        Vector2 end = start
            + Vector2.up * _upwardTravelDistance
            + toCenter * _centerTravelDistance
            + Vector2.right * sideDrift;

        float size = _particleSize + RandomRange(-_sizeVariation, _sizeVariation);
        if (size < 0f)
        {
            size = 0f;
        }

        float lifetime = _lifetimeSeconds + RandomRange(-_lifetimeVariationSeconds, _lifetimeVariationSeconds);
        if (lifetime <= 0.01f)
        {
            lifetime = 0.01f;
        }

        return new Particle
        {
            Start = start,
            End = end,
            Age = 0f,
            Lifetime = lifetime,
            Size = size,
            Color = GetParticleColor(start.x, rect),
            Phase = RandomRange(0f, Mathf.PI * 2f)
        };
    }

    private Vector2 GetRandomStartPosition(Rect rect)
    {
        float padding = _spawnPadding;
        float x = RandomRange(rect.xMin + padding, rect.xMax - padding);
        float y = RandomRange(rect.yMin + padding, rect.yMax - padding);
        return new Vector2(x, y);
    }

    private Color GetParticleColor(float x, Rect rect)
    {
        float centerX = rect.center.x;
        float blendWidth = _centerBlendWidth;

        if (blendWidth <= 0f)
        {
            return x < centerX ? _leftParticleColor : _rightParticleColor;
        }

        float distanceFromCenter = Mathf.Abs(x - centerX);
        if (distanceFromCenter >= blendWidth)
        {
            return x < centerX ? _leftParticleColor : _rightParticleColor;
        }

        float t = Clamp01(distanceFromCenter / blendWidth);
        Color sideColor = x < centerX ? _leftParticleColor : _rightParticleColor;
        return Color.Lerp(_centerBlendColor, sideColor, t);
    }

    private Vector2 GetSoftDriftOffset(Particle particle, float progress)
    {
        float wave = Mathf.Sin(progress * Mathf.PI * 2f + particle.Phase);
        float verticalWave = Mathf.Cos(progress * Mathf.PI + particle.Phase);
        return new Vector2(wave * _horizontalDrift * 0.22f, verticalWave * _horizontalDrift * 0.08f);
    }

    private float GetDeltaTime()
    {
        if (Application.isPlaying)
        {
            return Time.unscaledDeltaTime;
        }

#if UNITY_EDITOR
        double currentTime = UnityEditor.EditorApplication.timeSinceStartup;
        if (_lastEditorTime <= 0d)
        {
            _lastEditorTime = currentTime;
            return 0f;
        }

        double delta = currentTime - _lastEditorTime;
        _lastEditorTime = currentTime;

        if (delta < 0d)
        {
            return 0f;
        }

        if (delta > 0.1d)
        {
            delta = 0.1d;
        }

        return (float)delta;
#else
        return 0f;
#endif
    }

    private float RandomRange(float minValue, float maxValue)
    {
        if (_random == null)
        {
            _random = new System.Random(_randomSeed);
        }

        if (maxValue < minValue)
        {
            float swap = minValue;
            minValue = maxValue;
            maxValue = swap;
        }

        double value = _random.NextDouble();
        return minValue + (float)value * (maxValue - minValue);
    }

    private static void AddParticleQuad(VertexHelper vertexHelper, Vector2 center, float size, Color color)
    {
        if (size <= 0f || color.a <= 0f)
        {
            return;
        }

        float halfSize = size * 0.5f;
        int startIndex = vertexHelper.currentVertCount;

        AddVertex(vertexHelper, new Vector2(center.x - halfSize, center.y - halfSize), color);
        AddVertex(vertexHelper, new Vector2(center.x - halfSize, center.y + halfSize), color);
        AddVertex(vertexHelper, new Vector2(center.x + halfSize, center.y + halfSize), color);
        AddVertex(vertexHelper, new Vector2(center.x + halfSize, center.y - halfSize), color);

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

    private static float EaseOutCubic(float t)
    {
        t = Clamp01(t);
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    private static float SmoothStep(float edge0, float edge1, float value)
    {
        float t = SafeDivide(value - edge0, edge1 - edge0);
        t = Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a *= Clamp01(alpha);
        return color;
    }

    private static bool ApproximatelySameRect(Rect a, Rect b)
    {
        return Mathf.Abs(a.x - b.x) < 0.5f
            && Mathf.Abs(a.y - b.y) < 0.5f
            && Mathf.Abs(a.width - b.width) < 0.5f
            && Mathf.Abs(a.height - b.height) < 0.5f;
    }
}
