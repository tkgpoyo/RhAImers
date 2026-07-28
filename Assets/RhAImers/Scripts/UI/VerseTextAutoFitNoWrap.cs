using TMPro;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
[AddComponentMenu("UI/Verse Text Auto Fit No Wrap")]
public sealed class VerseTextAutoFitNoWrap : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private TMP_Text _text;
    [SerializeField] private RectTransform _fitArea;

    [Header("Text Layout")]
    [SerializeField] private bool _disableAutomaticWrapping = true;
    [SerializeField] private bool _collapseExplicitLineBreaks = false;
    [SerializeField] private TextOverflowModes _overflowModeAfterFit = TextOverflowModes.Overflow;

    [Header("Font Size")]
    [SerializeField] private bool _useCurrentFontSizeAsLargest = true;
    [SerializeField] private float _largestFontSize = 32f;
    [SerializeField] private float _smallestFontSize = 12f;
    [SerializeField] private int _searchStepCount = 12;

    [Header("Padding")]
    [SerializeField] private Vector2 _innerPadding = new Vector2(8f, 4f);

    [Header("Update")]
    [SerializeField] private bool _fitEveryFrame = true;
    [SerializeField] private bool _fitInEditMode = true;
    [SerializeField] private bool _forceMeshUpdateAfterFit = true;

    [SerializeField, HideInInspector] private float _capturedLargestFontSize = -1f;

    private RectTransform _textRectTransform;
    private string _lastText;
    private Vector2 _lastAreaSize;
    private bool _fitRequested = true;

    private void Reset()
    {
        ResolveReferences();
        CaptureCurrentFontAsLargest();
    }

    private void Awake()
    {
        ResolveReferences();
        CaptureCurrentFontAsLargestIfNeeded();
        ConfigureTextLayout();
        _fitRequested = true;
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureCurrentFontAsLargestIfNeeded();
        ConfigureTextLayout();
        _fitRequested = true;
    }

    private void OnValidate()
    {
        ResolveReferences();
        _fitRequested = true;
    }

    private void OnRectTransformDimensionsChange()
    {
        _fitRequested = true;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying && !_fitInEditMode)
        {
            return;
        }

        ResolveReferences();

        if (!ShouldFitNow())
        {
            return;
        }

        FitNow();
    }

    [ContextMenu("Fit Now")]
    public void FitNow()
    {
        ResolveReferences();

        if (_text == null)
        {
            return;
        }

        ConfigureTextLayout();

        if (_collapseExplicitLineBreaks)
        {
            string collapsedText = CollapseLineBreaks(_text.text);
            if (_text.text != collapsedText)
            {
                _text.text = collapsedText;
            }
        }

        RectTransform areaTransform = GetAreaTransform();
        if (areaTransform == null)
        {
            return;
        }

        Rect areaRect = areaTransform.rect;
        float allowedWidth = areaRect.width - _innerPadding.x * 2f;
        float allowedHeight = areaRect.height - _innerPadding.y * 2f;

        if (allowedWidth <= 1f || allowedHeight <= 1f)
        {
            return;
        }

        string value = _text.text;
        if (string.IsNullOrEmpty(value))
        {
            ApplyFontSize(GetLargestFontSize());
            StoreLastState(areaRect.size);
            return;
        }

        float largest = GetLargestFontSize();
        float smallest = GetSmallestFontSize(largest);

        float fittedSize;
        if (DoesTextFit(value, largest, allowedWidth, allowedHeight))
        {
            fittedSize = largest;
        }
        else
        {
            fittedSize = FindFittingFontSize(value, smallest, largest, allowedWidth, allowedHeight);
        }

        ApplyFontSize(fittedSize);
        StoreLastState(areaRect.size);
    }

    [ContextMenu("Capture Current Font As Largest")]
    public void CaptureCurrentFontAsLargest()
    {
        ResolveReferences();

        if (_text == null)
        {
            return;
        }

        _capturedLargestFontSize = _text.fontSize;
        _largestFontSize = _capturedLargestFontSize;
        _fitRequested = true;
    }

    private void ResolveReferences()
    {
        if (_text == null)
        {
            _text = GetComponent<TMP_Text>();
        }

        if (_textRectTransform == null && _text != null)
        {
            _textRectTransform = _text.rectTransform;
        }
    }

    private void CaptureCurrentFontAsLargestIfNeeded()
    {
        if (!_useCurrentFontSizeAsLargest)
        {
            return;
        }

        if (_capturedLargestFontSize > 0f)
        {
            return;
        }

        if (_text != null)
        {
            _capturedLargestFontSize = _text.fontSize;
        }
    }

    private void ConfigureTextLayout()
    {
        if (_text == null)
        {
            return;
        }

        _text.richText = true;
        _text.enableAutoSizing = false;
        _text.overflowMode = _overflowModeAfterFit;

        if (_disableAutomaticWrapping)
        {
            _text.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    private bool ShouldFitNow()
    {
        if (_text == null)
        {
            return false;
        }

        RectTransform areaTransform = GetAreaTransform();
        Vector2 areaSize = areaTransform != null ? areaTransform.rect.size : Vector2.zero;

        if (_fitEveryFrame || _fitRequested)
        {
            return true;
        }

        if (_lastText != _text.text)
        {
            return true;
        }

        return Mathf.Abs(_lastAreaSize.x - areaSize.x) > 0.5f
            || Mathf.Abs(_lastAreaSize.y - areaSize.y) > 0.5f;
    }

    private RectTransform GetAreaTransform()
    {
        if (_fitArea != null)
        {
            return _fitArea;
        }

        return _textRectTransform;
    }

    private float GetLargestFontSize()
    {
        float largest = _useCurrentFontSizeAsLargest && _capturedLargestFontSize > 0f
            ? _capturedLargestFontSize
            : _largestFontSize;

        if (largest <= 1f)
        {
            largest = 1f;
        }

        return largest;
    }

    private float GetSmallestFontSize(float largest)
    {
        float smallest = _smallestFontSize;

        if (smallest <= 1f)
        {
            smallest = 1f;
        }

        if (smallest > largest)
        {
            smallest = largest;
        }

        return smallest;
    }

    private float FindFittingFontSize(
        string value,
        float smallest,
        float largest,
        float allowedWidth,
        float allowedHeight)
    {
        float low = smallest;
        float high = largest;
        int steps = _searchStepCount;

        if (steps < 1)
        {
            steps = 1;
        }

        for (int i = 0; i < steps; i++)
        {
            float middle = (low + high) * 0.5f;

            if (DoesTextFit(value, middle, allowedWidth, allowedHeight))
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private bool DoesTextFit(string value, float fontSize, float allowedWidth, float allowedHeight)
    {
        if (_text == null)
        {
            return true;
        }

        float previousSize = _text.fontSize;
        _text.fontSize = fontSize;

        Vector2 preferred = _text.GetPreferredValues(value, 100000f, 100000f);

        _text.fontSize = previousSize;

        return preferred.x <= allowedWidth && preferred.y <= allowedHeight;
    }

    private void ApplyFontSize(float fontSize)
    {
        if (_text == null)
        {
            return;
        }

        _text.fontSize = fontSize;

        if (_forceMeshUpdateAfterFit)
        {
            _text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: false);
        }
    }

    private void StoreLastState(Vector2 areaSize)
    {
        _lastText = _text != null ? _text.text : string.Empty;
        _lastAreaSize = areaSize;
        _fitRequested = false;
    }

    private static string CollapseLineBreaks(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ");
    }
}
