using UnityEngine;

namespace Data.Managers.AnalyticsComponents
{
    public class AnalyticsWinComponent : MonoBehaviour
    {
        // private void Awake()
        // {
        //     Messenger<WinLoseManager.WinData>.AddListener(GameEvents.WinEvent, OnLevelWin);
        // }
        //
        // private void OnDestroy()
        // {
        //     Messenger<WinLoseManager.WinData>.RemoveListener(GameEvents.WinEvent, OnLevelWin);
        // }
        //
        // private void OnLevelWin(WinLoseManager.WinData data)
        // {
        //     AnalyticsManager.WinGameEvent($"{Game.Factions.CurrentFaction}:{data.LevelName}");
        // }
    }
}