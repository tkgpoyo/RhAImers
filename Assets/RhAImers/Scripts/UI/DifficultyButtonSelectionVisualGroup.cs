using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DifficultyButtonSelectionVisualGroup : MonoBehaviour
{
    private enum DifficultyButtonKind
    {
        Easy,
        Normal,
        Hard
    }

    [Header("Buttons")]
    [SerializeField] private Button _easyButton;
    [SerializeField] private Button _normalButton;
    [SerializeField] private Button _hardButton;

    [Header("Initial Selection")]
    [SerializeField] private DifficultyButtonKind _initialSelection = DifficultyButtonKind.Normal;

    [Header("Scale")]
    [SerializeField] private float _selectedScaleMultiplier = 1f;
    [SerializeField] private float _unselectedScaleMultiplier = 0.92f;
    [SerializeField] private float _animationDuration = 0.16f;
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Color")]
    [SerializeField] private Color _selectedTint = Color.white;
    [SerializeField] private Color _unselectedTint = new Color(0.48f, 0.48f, 0.58f, 0.78f);
    [SerializeField] private float _colorSmoothing = 16f;

    [Header("Behavior")]
    [SerializeField] private bool _captureRestPoseOnEnable = true;
    [SerializeField] private bool _applyInitialSelectionOnEnable = true;

    private ButtonState _easyState;
    private ButtonState _normalState;
    private ButtonState _hardState;
    private DifficultyButtonKind _selectedKind;
    private bool _hasSelectedKind;

    private sealed class ButtonState
    {
        public Button Button;
        public RectTransform RectTransform;
        public Graphic Graphic;
        public UiButtonHoverScaleSoundEffect HoverScaleEffect;
        public Vector3 RestScale = Vector3.one;
        public Color RestColor = Color.white;
        public Vector3 ScaleVelocity;
        public float CurrentBaseScale = 1f;
        public float TargetBaseScale = 1f;
        public Color TargetColor = Color.white;
        public bool HasRestPose;
    }

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        CaptureRestPosesIfNeeded(force: true);
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureRestPosesIfNeeded(force: _captureRestPoseOnEnable);
        Subscribe();

        if (_applyInitialSelectionOnEnable || !_hasSelectedKind)
        {
            SetSelected(_initialSelection, instant: true, ignoreIfAlreadySelected: false);
        }
        else
        {
            ApplyTargetsForSelection(_selectedKind, instant: true);
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void Update()
    {
        float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        UpdateState(_easyState, deltaTime);
        UpdateState(_normalState, deltaTime);
        UpdateState(_hardState, deltaTime);
    }

    private void HandleEasyClicked()
    {
        SetSelected(DifficultyButtonKind.Easy, instant: false, ignoreIfAlreadySelected: true);
    }

    private void HandleNormalClicked()
    {
        SetSelected(DifficultyButtonKind.Normal, instant: false, ignoreIfAlreadySelected: true);
    }

    private void HandleHardClicked()
    {
        SetSelected(DifficultyButtonKind.Hard, instant: false, ignoreIfAlreadySelected: true);
    }

    [ContextMenu("Select Easy")]
    private void SelectEasyFromContextMenu()
    {
        SetSelected(DifficultyButtonKind.Easy, instant: false, ignoreIfAlreadySelected: true);
    }

    [ContextMenu("Select Normal")]
    private void SelectNormalFromContextMenu()
    {
        SetSelected(DifficultyButtonKind.Normal, instant: false, ignoreIfAlreadySelected: true);
    }

    [ContextMenu("Select Hard")]
    private void SelectHardFromContextMenu()
    {
        SetSelected(DifficultyButtonKind.Hard, instant: false, ignoreIfAlreadySelected: true);
    }

    [ContextMenu("Capture Current Button Looks As Rest Pose")]
    private void CaptureCurrentLooksAsRestPose()
    {
        CaptureRestPosesIfNeeded(force: true);
        ApplyTargetsForSelection(_hasSelectedKind ? _selectedKind : _initialSelection, instant: true);
    }

    private void SetSelected(DifficultyButtonKind kind, bool instant, bool ignoreIfAlreadySelected)
    {
        if (ignoreIfAlreadySelected && _hasSelectedKind && _selectedKind == kind)
        {
            return;
        }

        _selectedKind = kind;
        _hasSelectedKind = true;
        ApplyTargetsForSelection(kind, instant);
    }

    private void ApplyTargetsForSelection(DifficultyButtonKind selectedKind, bool instant)
    {
        ApplyTarget(_easyState, selectedKind == DifficultyButtonKind.Easy, instant);
        ApplyTarget(_normalState, selectedKind == DifficultyButtonKind.Normal, instant);
        ApplyTarget(_hardState, selectedKind == DifficultyButtonKind.Hard, instant);
    }

    private void ApplyTarget(ButtonState state, bool selected, bool instant)
    {
        if (state == null)
        {
            return;
        }

        state.TargetBaseScale = selected ? _selectedScaleMultiplier : _unselectedScaleMultiplier;
        Color tint = selected ? _selectedTint : _unselectedTint;
        state.TargetColor = MultiplyColor(state.RestColor, tint);

        if (!instant)
        {
            return;
        }

        state.CurrentBaseScale = state.TargetBaseScale;
        state.ScaleVelocity = Vector3.zero;

        if (state.HoverScaleEffect != null)
        {
            state.HoverScaleEffect.BaseScaleMultiplier = state.CurrentBaseScale;
        }
        else if (state.RectTransform != null)
        {
            state.RectTransform.localScale = state.RestScale * state.CurrentBaseScale;
        }

        if (state.Graphic != null)
        {
            state.Graphic.color = state.TargetColor;
        }
    }

    private void UpdateState(ButtonState state, float deltaTime)
    {
        if (state == null || !state.HasRestPose)
        {
            return;
        }

        if (_animationDuration <= 0f || deltaTime <= 0f)
        {
            state.CurrentBaseScale = state.TargetBaseScale;
        }
        else
        {
            Vector3 current = Vector3.one * state.CurrentBaseScale;
            Vector3 target = Vector3.one * state.TargetBaseScale;
            Vector3 next = Vector3.SmoothDamp(
                current,
                target,
                ref state.ScaleVelocity,
                _animationDuration,
                Mathf.Infinity,
                deltaTime);

            state.CurrentBaseScale = next.x;
        }

        if (state.HoverScaleEffect != null)
        {
            state.HoverScaleEffect.BaseScaleMultiplier = state.CurrentBaseScale;
        }
        else if (state.RectTransform != null)
        {
            state.RectTransform.localScale = state.RestScale * state.CurrentBaseScale;
        }

        if (state.Graphic != null)
        {
            if (_colorSmoothing <= 0f || deltaTime <= 0f)
            {
                state.Graphic.color = state.TargetColor;
            }
            else
            {
                float t = 1f - Mathf.Exp(-_colorSmoothing * deltaTime);
                state.Graphic.color = Color.Lerp(state.Graphic.color, state.TargetColor, Clamp01(t));
            }
        }
    }

    private void ResolveReferences()
    {
        _easyState = BuildState(_easyState, _easyButton);
        _normalState = BuildState(_normalState, _normalButton);
        _hardState = BuildState(_hardState, _hardButton);
    }

    private ButtonState BuildState(ButtonState previous, Button button)
    {
        ButtonState state = previous ?? new ButtonState();
        state.Button = button;

        if (button == null)
        {
            state.RectTransform = null;
            state.Graphic = null;
            state.HoverScaleEffect = null;
            return state;
        }

        state.RectTransform = button.transform as RectTransform;
        state.Graphic = button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();
        state.HoverScaleEffect = button.GetComponent<UiButtonHoverScaleSoundEffect>();
        return state;
    }

    private void CaptureRestPosesIfNeeded(bool force)
    {
        CaptureRestPose(_easyState, force);
        CaptureRestPose(_normalState, force);
        CaptureRestPose(_hardState, force);
    }

    private void CaptureRestPose(ButtonState state, bool force)
    {
        if (state == null || state.RectTransform == null)
        {
            return;
        }

        if (state.HasRestPose && !force)
        {
            return;
        }

        state.RestScale = state.RectTransform.localScale;
        state.RestColor = state.Graphic != null ? state.Graphic.color : Color.white;
        state.CurrentBaseScale = 1f;
        state.TargetBaseScale = 1f;
        state.ScaleVelocity = Vector3.zero;
        state.HasRestPose = true;

        if (state.HoverScaleEffect != null)
        {
            state.HoverScaleEffect.BaseScaleMultiplier = 1f;
        }
    }

    private void Subscribe()
    {
        Unsubscribe();

        if (_easyButton != null)
        {
            _easyButton.onClick.AddListener(HandleEasyClicked);
        }

        if (_normalButton != null)
        {
            _normalButton.onClick.AddListener(HandleNormalClicked);
        }

        if (_hardButton != null)
        {
            _hardButton.onClick.AddListener(HandleHardClicked);
        }
    }

    private void Unsubscribe()
    {
        if (_easyButton != null)
        {
            _easyButton.onClick.RemoveListener(HandleEasyClicked);
        }

        if (_normalButton != null)
        {
            _normalButton.onClick.RemoveListener(HandleNormalClicked);
        }

        if (_hardButton != null)
        {
            _hardButton.onClick.RemoveListener(HandleHardClicked);
        }
    }

    private static Color MultiplyColor(Color baseColor, Color tint)
    {
        return new Color(
            baseColor.r * tint.r,
            baseColor.g * tint.g,
            baseColor.b * tint.b,
            baseColor.a * tint.a);
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
