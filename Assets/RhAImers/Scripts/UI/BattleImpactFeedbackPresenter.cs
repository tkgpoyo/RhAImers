using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    /// <summary>
    /// プレイヤーバースの衝突イベントに合わせて、
    /// 画面フラッシュとカメラ／UIシェイクを再生します。
    ///
    /// GameManagerには依存せず、UIManager.PlayerVerseImpactOccurredを購読します。
    /// </summary>
    public class BattleImpactFeedbackPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private Canvas _battleCanvas;

        [Header("Flash Overlay")]
        [SerializeField] private Image _flashImage;
        [SerializeField] private CanvasGroup _flashCanvasGroup;
        [SerializeField] private bool _autoCreateFlashOverlay = true;
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField, Min(1)] private int _flashCount = 3;
        [SerializeField, Min(0f)] private float _flashInDurationSec = 0.025f;
        [SerializeField, Min(0f)] private float _flashOutDurationSec = 0.055f;
        [SerializeField, Min(0f)] private float _flashIntervalSec = 0.025f;
        [SerializeField, Range(0f, 1f)] private float _flashPeakAlpha = 0.82f;

        [Header("Camera Shake")]
        [SerializeField] private Transform _cameraShakeTarget;
        [SerializeField] private bool _autoUseMainCamera = true;
        [SerializeField] private bool _shakeCamera = true;
        [SerializeField] private Vector2 _cameraShakeAmplitude = new Vector2(0.08f, 0.05f);

        [Header("UI Shake")]
        [SerializeField] private RectTransform _uiShakeTarget;
        [SerializeField] private bool _shakeUi = false;
        [SerializeField] private Vector2 _uiShakeAmplitude = new Vector2(10f, 6f);

        [Header("Shake Timing")]
        [SerializeField, Min(0f)] private float _shakeDurationSec = 0.28f;
        [SerializeField, Min(0.1f)] private float _shakeFrequency = 36f;
        [SerializeField] private AnimationCurve _shakeDecay =
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        private CancellationTokenSource _feedbackCancellationTokenSource;

        private Vector3 _lastCameraOffset;
        private Vector2 _lastUiOffset;

        private void Awake()
        {
            ResolveReferences();
            PrepareFlashOverlay();
            ResetFeedbackVisuals();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_uiManager != null)
            {
                _uiManager.PlayerVerseImpactOccurred += HandlePlayerVerseImpactOccurred;
            }
        }

        private void OnDisable()
        {
            if (_uiManager != null)
            {
                _uiManager.PlayerVerseImpactOccurred -= HandlePlayerVerseImpactOccurred;
            }

            CancelFeedback();
            ResetFeedbackVisuals();
        }

        private void HandlePlayerVerseImpactOccurred()
        {
            PlayFeedbackAsync().Forget(ex =>
            {
                if (ex is OperationCanceledException)
                {
                    return;
                }

                Debug.LogException(ex, this);
            });
        }

        private async UniTask PlayFeedbackAsync()
        {
            CancelFeedback();
            ResolveReferences();
            PrepareFlashOverlay();

            var cancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    destroyCancellationToken
                );

            _feedbackCancellationTokenSource =
                cancellationTokenSource;

            CancellationToken ct =
                cancellationTokenSource.Token;

            try
            {
                await UniTask.WhenAll(
                    PlayFlashSequenceAsync(ct),
                    PlayScreenShakeAsync(ct)
                );
            }
            finally
            {
                RestoreShakeTargets();
                SetFlashAlpha(0f);

                cancellationTokenSource.Dispose();

                if (_feedbackCancellationTokenSource ==
                    cancellationTokenSource)
                {
                    _feedbackCancellationTokenSource = null;
                }
            }
        }

        private async UniTask PlayFlashSequenceAsync(
            CancellationToken ct)
        {
            if (_flashCanvasGroup == null ||
                _flashCount <= 0)
            {
                return;
            }

            for (int i = 0; i < _flashCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                await AnimateFlashAlphaAsync(
                    0f,
                    _flashPeakAlpha,
                    _flashInDurationSec,
                    ct
                );

                await AnimateFlashAlphaAsync(
                    _flashPeakAlpha,
                    0f,
                    _flashOutDurationSec,
                    ct
                );

                if (i < _flashCount - 1 &&
                    _flashIntervalSec > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(
                            _flashIntervalSec
                        ),
                        ignoreTimeScale: true,
                        cancellationToken: ct
                    );
                }
            }

            SetFlashAlpha(0f);
        }

        private async UniTask AnimateFlashAlphaAsync(
            float from,
            float to,
            float duration,
            CancellationToken ct)
        {
            if (_flashCanvasGroup == null)
            {
                return;
            }

            if (duration <= 0f)
            {
                SetFlashAlpha(to);
                return;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(
                    elapsed / duration
                );

                float eased = EaseOutCubic(t);
                SetFlashAlpha(
                    Mathf.Lerp(from, to, eased)
                );

                await UniTask.Yield(
                    PlayerLoopTiming.Update,
                    ct
                );
            }

            SetFlashAlpha(to);
        }

        private async UniTask PlayScreenShakeAsync(
            CancellationToken ct)
        {
            bool hasCameraTarget =
                _shakeCamera &&
                _cameraShakeTarget != null;

            bool hasUiTarget =
                _shakeUi &&
                _uiShakeTarget != null;

            if (_shakeDurationSec <= 0f ||
                (!hasCameraTarget && !hasUiTarget))
            {
                return;
            }

            _lastCameraOffset = Vector3.zero;
            _lastUiOffset = Vector2.zero;

            float elapsed = 0f;
            float seedX = UnityEngine.Random.Range(0f, 1000f);
            float seedY = UnityEngine.Random.Range(0f, 1000f);

            while (elapsed < _shakeDurationSec)
            {
                ct.ThrowIfCancellationRequested();

                // 前フレームで加えたオフセットだけを除去する。
                // 他スクリプトによる移動は極力維持する。
                RemovePreviousShakeOffsets();

                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(
                    elapsed / _shakeDurationSec
                );

                float decay = EvaluateCurve(
                    _shakeDecay,
                    normalized,
                    1f - normalized
                );

                float noiseTime =
                    Time.unscaledTime *
                    Mathf.Max(0.1f, _shakeFrequency);

                float noiseX =
                    Mathf.PerlinNoise(seedX, noiseTime) *
                    2f - 1f;

                float noiseY =
                    Mathf.PerlinNoise(seedY, noiseTime) *
                    2f - 1f;

                if (hasCameraTarget)
                {
                    _lastCameraOffset =
                        new Vector3(
                            noiseX *
                            _cameraShakeAmplitude.x *
                            decay,
                            noiseY *
                            _cameraShakeAmplitude.y *
                            decay,
                            0f
                        );

                    _cameraShakeTarget.localPosition +=
                        _lastCameraOffset;
                }

                if (hasUiTarget)
                {
                    _lastUiOffset =
                        new Vector2(
                            noiseX *
                            _uiShakeAmplitude.x *
                            decay,
                            noiseY *
                            _uiShakeAmplitude.y *
                            decay
                        );

                    _uiShakeTarget.anchoredPosition +=
                        _lastUiOffset;
                }

                await UniTask.Yield(
                    PlayerLoopTiming.Update,
                    ct
                );
            }

            RemovePreviousShakeOffsets();
        }

        private void ResolveReferences()
        {
            if (_uiManager == null)
            {
                _uiManager = GetComponent<UIManager>();
            }

            if (_uiManager == null)
            {
                _uiManager =
                    GetComponentInParent<UIManager>();
            }

            if (_uiManager == null)
            {
                _uiManager =
                    FindObjectOfType<UIManager>();
            }

            if (_battleCanvas == null)
            {
                _battleCanvas =
                    GetComponentInParent<Canvas>();
            }

            if (_battleCanvas == null)
            {
                _battleCanvas =
                    FindObjectOfType<Canvas>();
            }

            if (_cameraShakeTarget == null &&
                _autoUseMainCamera &&
                Camera.main != null)
            {
                _cameraShakeTarget =
                    Camera.main.transform;
            }
        }

        private void PrepareFlashOverlay()
        {
            if (_flashImage == null &&
                _autoCreateFlashOverlay &&
                _battleCanvas != null)
            {
                GameObject overlayObject =
                    new GameObject(
                        "BattleImpactFlashOverlay",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image),
                        typeof(CanvasGroup)
                    );

                overlayObject.transform.SetParent(
                    _battleCanvas.transform,
                    false
                );

                RectTransform rectTransform =
                    overlayObject.GetComponent<RectTransform>();

                rectTransform.anchorMin =
                    Vector2.zero;

                rectTransform.anchorMax =
                    Vector2.one;

                rectTransform.offsetMin =
                    Vector2.zero;

                rectTransform.offsetMax =
                    Vector2.zero;

                rectTransform.localScale =
                    Vector3.one;

                _flashImage =
                    overlayObject.GetComponent<Image>();

                _flashCanvasGroup =
                    overlayObject.GetComponent<CanvasGroup>();

                overlayObject.transform.SetAsLastSibling();
            }

            if (_flashImage != null &&
                _flashCanvasGroup == null)
            {
                _flashCanvasGroup =
                    _flashImage.GetComponent<CanvasGroup>();
            }

            if (_flashImage != null &&
                _flashCanvasGroup == null)
            {
                _flashCanvasGroup =
                    _flashImage.gameObject
                        .AddComponent<CanvasGroup>();
            }

            if (_flashImage != null)
            {
                Color color = _flashColor;
                color.a = 1f;
                _flashImage.color = color;
                _flashImage.raycastTarget = false;
            }

            if (_flashCanvasGroup != null)
            {
                _flashCanvasGroup.interactable = false;
                _flashCanvasGroup.blocksRaycasts = false;
                _flashCanvasGroup.alpha = 0f;
            }
        }

        private void CancelFeedback()
        {
            if (_feedbackCancellationTokenSource == null)
            {
                return;
            }

            _feedbackCancellationTokenSource.Cancel();
            _feedbackCancellationTokenSource.Dispose();
            _feedbackCancellationTokenSource = null;
        }

        private void ResetFeedbackVisuals()
        {
            RestoreShakeTargets();
            SetFlashAlpha(0f);
        }

        private void RestoreShakeTargets()
        {
            RemovePreviousShakeOffsets();
        }

        private void RemovePreviousShakeOffsets()
        {
            if (_cameraShakeTarget != null &&
                _lastCameraOffset != Vector3.zero)
            {
                _cameraShakeTarget.localPosition -=
                    _lastCameraOffset;
            }

            if (_uiShakeTarget != null &&
                _lastUiOffset != Vector2.zero)
            {
                _uiShakeTarget.anchoredPosition -=
                    _lastUiOffset;
            }

            _lastCameraOffset = Vector3.zero;
            _lastUiOffset = Vector2.zero;
        }

        private void SetFlashAlpha(float alpha)
        {
            if (_flashCanvasGroup == null)
            {
                return;
            }

            _flashCanvasGroup.alpha =
                Mathf.Clamp01(alpha);
        }

        private static float EvaluateCurve(
            AnimationCurve curve,
            float normalized,
            float fallback)
        {
            if (curve == null || curve.length == 0)
            {
                return fallback;
            }

            return curve.Evaluate(normalized);
        }

        private static float EaseOutCubic(float t)
        {
            float inverted = 1f - Mathf.Clamp01(t);
            return 1f - inverted * inverted * inverted;
        }
    }
}
