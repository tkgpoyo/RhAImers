using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RhAImers.UI
{
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class TutorialSpotlightOverlay : MonoBehaviour, ICanvasRaycastFilter
    {
        public float fadeSeconds = 0.35f;
        public float darkness = 0.76f;
        public float padding = 12f;
        private RectTransform _root;
        private RectTransform _target;
        private readonly Image[] _shades = new Image[4];
        private readonly Vector3[] _corners = new Vector3[4];
        private Rect _hole;
        private bool _hasHole;
        private bool _allowInput;
        private float _alpha;
        private Coroutine _fade;

        private void Awake()
        {
            _root = (RectTransform)transform;
            Image blocker = GetComponent<Image>();
            blocker.color = Color.clear;
            blocker.raycastTarget = true;
            for (int i = 0; i < _shades.Length; i++)
            {
                var go = new GameObject("Shade_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
                _shades[i] = go.GetComponent<Image>();
                _shades[i].color = Color.clear;
                _shades[i].raycastTarget = false;
            }
        }

        public void Focus(RectTransform target, bool allowInput = false)
        {
            _target = target;
            _allowInput = allowInput;
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeIn());
            UpdateShades();
        }

        private IEnumerator FadeIn()
        {
            float from = _alpha;
            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = fadeSeconds > 0f ? Mathf.Clamp01(elapsed / fadeSeconds) : 1f;
                _alpha = Mathf.Lerp(from, darkness, t);
                yield return null;
            }
            _alpha = darkness;
            _fade = null;
        }

        private void LateUpdate() => UpdateShades();

        private void UpdateShades()
        {
            if (_root == null) return;
            Rect r = _root.rect;
            _hasHole = _target != null && _target.gameObject.activeInHierarchy;
            if (_hasHole)
            {
                _target.GetWorldCorners(_corners);
                Canvas source = _target.GetComponentInParent<Canvas>();
                Camera sourceCamera = source != null && source.renderMode != RenderMode.ScreenSpaceOverlay ? source.worldCamera : null;
                Canvas overlay = GetComponentInParent<Canvas>();
                Camera overlayCamera = overlay != null && overlay.renderMode != RenderMode.ScreenSpaceOverlay ? overlay.worldCamera : null;
                Vector2 lo = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                Vector2 hi = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                foreach (Vector3 corner in _corners)
                {
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, corner);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, overlayCamera, out Vector2 local);
                    lo = Vector2.Min(lo, local);
                    hi = Vector2.Max(hi, local);
                }
                _hole = Rect.MinMaxRect(Mathf.Clamp(lo.x - padding, r.xMin, r.xMax), Mathf.Clamp(lo.y - padding, r.yMin, r.yMax),
                    Mathf.Clamp(hi.x + padding, r.xMin, r.xMax), Mathf.Clamp(hi.y + padding, r.yMin, r.yMax));
                SetShade(0, Rect.MinMaxRect(r.xMin, _hole.yMax, r.xMax, r.yMax));
                SetShade(1, Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, _hole.yMin));
                SetShade(2, Rect.MinMaxRect(r.xMin, _hole.yMin, _hole.xMin, _hole.yMax));
                SetShade(3, Rect.MinMaxRect(_hole.xMax, _hole.yMin, r.xMax, _hole.yMax));
            }
            else
            {
                SetShade(0, r);
                for (int i = 1; i < 4; i++) SetShade(i, Rect.zero);
            }
        }

        private void SetShade(int index, Rect rect)
        {
            Image shade = _shades[index];
            shade.color = new Color(0f, 0f, 0f, _alpha);
            RectTransform rt = shade.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = rect.center;
            rt.sizeDelta = rect.size;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!_allowInput || !_hasHole) return true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPoint, eventCamera, out Vector2 local);
            return !_hole.Contains(local);
        }
    }
}
