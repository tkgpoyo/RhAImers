using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RhAImers.AvatorMotion
{
    public class RapperMotionController : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private Animator _rapperAnimator;
        [SerializeField] private string _combatStateName = "Combat";
        [SerializeField] private string _rapStateName = "rap";
        [SerializeField] private float _transitionDuration = 0.25f;

        public async UniTask CombatMotionAsync()
        {
            _rapperAnimator.CrossFadeInFixedTime(_combatStateName, _transitionDuration);
            // Combatモーションが再生し終わるくらいまで待機（例: 1.5秒）
            await UniTask.Delay(1500); 
        }

        public void StartRapMotion()
        {
            _rapperAnimator.CrossFadeInFixedTime(_rapStateName, _transitionDuration);
        }
    }
}