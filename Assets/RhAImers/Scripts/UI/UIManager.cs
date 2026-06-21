using RhAImers.Battle;
using UnityEngine;

namespace RhAImers.UI
{
    public class UIManager : MonoBehaviour
    {
        // TODO: 全てのUI系処理の実装が必要
        public void ShowTitle() { }
        public void ShowModeSelect() { }
        public void ShowOpponentVerse(Verse verse) { }
        public void ShowInputTimer(int sec) { }
        public void ShowInputRhymes(System.Collections.Generic.IReadOnlyList<string> rhymes) { }
        public void ShowGeneratedVerse(Verse verse) { }
        public void ShowResult(BattleResult result) { }
        public void ShowOpponentVerseLoading() { }
        public void ShowGenerationLoading() { }
        public void ShowScoringLoading() { }
        public void UpdateInputTimer(int sec) { }
    }
}