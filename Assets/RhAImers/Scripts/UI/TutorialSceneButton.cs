using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RhAImers.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class TutorialSceneButton : MonoBehaviour
    {
        public string targetSceneName = "TutorialScene";
        public bool buttonAlreadyFades;
        public float fadeSeconds = 0.45f;
        private Button _button;
        private bool _opening;

        private void Awake() => _button = GetComponent<Button>();
        private void OnEnable()
        {
            if (_button == null) _button = GetComponent<Button>();
            _button.onClick.AddListener(Open);
        }
        private void OnDisable() => _button.onClick.RemoveListener(Open);

        public void Open()
        {
            if (_opening) return;
            if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                Debug.LogError($"Tutorial scene is missing from the build scene list: {targetSceneName}", this);
                return;
            }
            _opening = true;
            if (buttonAlreadyFades) Load();
            else SceneFadeOverlay.Instance.PlayTransition(Color.black, fadeSeconds, fadeSeconds, 0.05f, true, Load);
        }

        private void Load()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
