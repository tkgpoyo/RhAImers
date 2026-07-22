using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/Fade Transition Button")]
public sealed class FadeTransitionButton : Button
{
    [Header("Scene Fade")]
    [SerializeField] private Color _fadeColor = Color.black;
    [SerializeField] private float _fadeOutDuration = 0.45f;
    [SerializeField] private float _fadeInDuration = 0.45f;
    [SerializeField] private float _fadeInDelay = 0.05f;
    [SerializeField] private bool _ignoreTimeScale = true;
    [SerializeField] private bool _temporarilyDisableInteractable = true;

    private bool _isTransitioning;

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        StartFadeTransition();
    }

    public override void OnSubmit(BaseEventData eventData)
    {
        StartFadeTransition();
    }

    private void StartFadeTransition()
    {
        if (_isTransitioning || !IsActive() || !IsInteractable())
        {
            return;
        }

        _isTransitioning = true;

        if (_temporarilyDisableInteractable)
        {
            interactable = false;
        }

        SceneFadeOverlay.Instance.PlayTransition(
            _fadeColor,
            _fadeOutDuration,
            _fadeInDuration,
            _fadeInDelay,
            _ignoreTimeScale,
            InvokeClickAfterFadeOut);
    }

    private void InvokeClickAfterFadeOut()
    {
        onClick.Invoke();
        StartCoroutine(RestoreInteractableIfStillAlive());
    }

    private IEnumerator RestoreInteractableIfStillAlive()
    {
        yield return null;

        if (this == null)
        {
            yield break;
        }

        _isTransitioning = false;

        if (_temporarilyDisableInteractable)
        {
            interactable = true;
        }
    }
}
