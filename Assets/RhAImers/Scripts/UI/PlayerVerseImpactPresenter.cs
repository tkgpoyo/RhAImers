using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RhAImers.UI
{
    /// <summary>
    /// プレイヤーバースのパネルを相手3Dモデルへ飛ばし、
    /// 縮小・消失させる衝突演出を担当します。
    /// </summary>
    public class PlayerVerseImpactPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _panelRectTransform;
        [SerializeField] private CanvasGroup _panelCanvasGroup;
        [SerializeField] private Canvas _battleCanvas;
        [SerializeField] private Camera _worldCamera;
        [SerializeField] private Transform _opponentImpactTarget;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _holdDurationSec = 0.25f;
        [SerializeField, Min(0f)] private float _travelDurationSec = 0.45f;

        [Header("Motion")]
        [SerializeField, Min(0.001f)] private float _endScale = 0.08f;
        [SerializeField] private float _arcHeight = 40f;
        [SerializeField, Range(0f, 1f)] private float _fadeStartNormalized = 0.65f;
        [SerializeField] private AnimationCurve _movementCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _scaleCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Visibility")]
        [SerializeField, Range(0f, 1f)] private float _visibleAlpha = 1f;
        [SerializeField] private bool _keepHiddenAfterImpact = true;

        private CancellationTokenSource _playCancellationTokenSource;

        private Vector2 _baselineAnchoredPosition;
        private Vector3 _baselineScale = Vector3.one;
        private bool _baselineCaptured;

        private void Awake()
        {
            ResolveReferences();
            CaptureBaseline();
        }

        private void OnDisable()
        {
            CancelAndRestore();
        }

        /// <summary>
        /// 次回表示前に、パネルの位置・大きさ・透明度を初期状態へ戻します。
        /// </summary>
        public void PrepareForShow()
        {
            ResolveReferences();

            if (!_baselineCaptured)
            {
                CaptureBaseline();
            }

            RestoreTransform();

            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha = _visibleAlpha;
                _panelCanvasGroup.interactable = false;
                _panelCanvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// パネル衝突演出を再生します。
        /// onImpactはパネルが相手へ到達して消失した瞬間に呼ばれます。
        /// </summary>
        public async UniTask PlayAsync(
            Action onImpact,
            CancellationToken cancellationToken = default)
        {
            CancelRunningAnimation(restoreVisible: false);
            ResolveReferences();

            if (_panelRectTransform == null)
            {
                onImpact?.Invoke();
                return;
            }

            Vector2 startPosition = _panelRectTransform.anchoredPosition;
            Vector3 startScale = _panelRectTransform.localScale;
            float startAlpha = _panelCanvasGroup != null
                ? _panelCanvasGroup.alpha
                : _visibleAlpha;

            if (!TryGetImpactAnchoredPosition(out Vector2 destination))
            {
                Debug.LogWarning(
                    $"{nameof(PlayerVerseImpactPresenter)}: " +
                    "Opponent Impact Target または Camera / Canvas の設定を確認してください。",
                    this
                );

                onImpact?.Invoke();
                return;
            }

            var playCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    destroyCancellationToken
                );

            _playCancellationTokenSource =
                playCancellationTokenSource;

            CancellationToken ct =
                playCancellationTokenSource.Token;

            bool impactOccurred = false;

            try
            {
                if (_holdDurationSec > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_holdDurationSec),
                        ignoreTimeScale: true,
                        cancellationToken: ct
                    );
                }

                if (_travelDurationSec <= 0f)
                {
                    ApplyImpactVisual(destination, startScale, 1f, startAlpha);
                }
                else
                {
                    float elapsed = 0f;

                    while (elapsed < _travelDurationSec)
                    {
                        ct.ThrowIfCancellationRequested();

                        elapsed += Time.unscaledDeltaTime;
                        float normalized =
                            Mathf.Clamp01(elapsed / _travelDurationSec);

                        float movementT = EvaluateCurve(
                            _movementCurve,
                            normalized
                        );

                        float scaleT = EvaluateCurve(
                            _scaleCurve,
                            normalized
                        );

                        Vector2 position =
                            Vector2.LerpUnclamped(
                                startPosition,
                                destination,
                                movementT
                            );

                        if (Mathf.Abs(_arcHeight) > 0.001f)
                        {
                            position.y +=
                                Mathf.Sin(normalized * Mathf.PI) *
                                _arcHeight;
                        }

                        _panelRectTransform.anchoredPosition = position;
                        _panelRectTransform.localScale =
                            Vector3.LerpUnclamped(
                                startScale,
                                startScale * _endScale,
                                scaleT
                            );

                        if (_panelCanvasGroup != null)
                        {
                            float fadeT = Mathf.InverseLerp(
                                _fadeStartNormalized,
                                1f,
                                normalized
                            );

                            _panelCanvasGroup.alpha =
                                Mathf.Lerp(startAlpha, 0f, fadeT);
                        }

                        await UniTask.Yield(
                            PlayerLoopTiming.Update,
                            ct
                        );
                    }

                    ApplyImpactVisual(
                        destination,
                        startScale,
                        1f,
                        startAlpha
                    );
                }

                impactOccurred = true;
                onImpact?.Invoke();
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                // Scene遷移・再実行・破棄による中断は正常終了とします。
            }
            finally
            {
                _panelRectTransform.anchoredPosition = startPosition;
                _panelRectTransform.localScale = startScale;

                if (_panelCanvasGroup != null)
                {
                    _panelCanvasGroup.alpha =
                        impactOccurred && _keepHiddenAfterImpact
                            ? 0f
                            : startAlpha;

                    _panelCanvasGroup.interactable = false;
                    _panelCanvasGroup.blocksRaycasts = false;
                }

                playCancellationTokenSource.Dispose();

                if (_playCancellationTokenSource ==
                    playCancellationTokenSource)
                {
                    _playCancellationTokenSource = null;
                }
            }
        }

        public void CancelAndRestore()
        {
            CancelRunningAnimation(restoreVisible: true);
        }

        private void ResolveReferences()
        {
            if (_panelRectTransform == null)
            {
                _panelRectTransform = transform as RectTransform;
            }

            if (_panelCanvasGroup == null)
            {
                _panelCanvasGroup = GetComponent<CanvasGroup>();
            }

            if (_panelCanvasGroup == null)
            {
                _panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (_battleCanvas == null)
            {
                _battleCanvas = GetComponentInParent<Canvas>();
            }

            if (_worldCamera == null)
            {
                _worldCamera = Camera.main;
            }
        }

        private void CaptureBaseline()
        {
            if (_panelRectTransform == null)
            {
                return;
            }

            _baselineAnchoredPosition =
                _panelRectTransform.anchoredPosition;

            _baselineScale =
                _panelRectTransform.localScale;

            _baselineCaptured = true;
        }

        private void RestoreTransform()
        {
            if (!_baselineCaptured || _panelRectTransform == null)
            {
                return;
            }

            _panelRectTransform.anchoredPosition =
                _baselineAnchoredPosition;

            _panelRectTransform.localScale =
                _baselineScale;
        }

        private void CancelRunningAnimation(bool restoreVisible)
        {
            if (_playCancellationTokenSource != null)
            {
                _playCancellationTokenSource.Cancel();
                _playCancellationTokenSource.Dispose();
                _playCancellationTokenSource = null;
            }

            RestoreTransform();

            if (restoreVisible && _panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha = _visibleAlpha;
                _panelCanvasGroup.interactable = false;
                _panelCanvasGroup.blocksRaycasts = false;
            }
        }

        private bool TryGetImpactAnchoredPosition(
            out Vector2 anchoredPosition)
        {
            anchoredPosition = Vector2.zero;

            if (_opponentImpactTarget == null ||
                _panelRectTransform == null ||
                !(_panelRectTransform.parent is RectTransform parentRect))
            {
                return false;
            }

            Camera worldCamera =
                _worldCamera != null
                    ? _worldCamera
                    : Camera.main;

            if (worldCamera == null)
            {
                return false;
            }

            Vector3 screenPoint =
                worldCamera.WorldToScreenPoint(
                    _opponentImpactTarget.position
                );

            if (screenPoint.z < 0f)
            {
                return false;
            }

            Camera canvasCamera = null;

            if (_battleCanvas != null &&
                _battleCanvas.renderMode !=
                RenderMode.ScreenSpaceOverlay)
            {
                canvasCamera = _battleCanvas.worldCamera;
            }

            return RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    parentRect,
                    screenPoint,
                    canvasCamera,
                    out anchoredPosition
                );
        }

        private void ApplyImpactVisual(
            Vector2 destination,
            Vector3 startScale,
            float normalized,
            float startAlpha)
        {
            _panelRectTransform.anchoredPosition = destination;
            _panelRectTransform.localScale =
                startScale * _endScale;

            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha = 0f;
            }
        }

        private static float EvaluateCurve(
            AnimationCurve curve,
            float normalized)
        {
            if (curve == null || curve.length == 0)
            {
                return normalized;
            }

            return curve.Evaluate(normalized);
        }
    }
}
