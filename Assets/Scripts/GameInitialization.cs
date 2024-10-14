using CardTD.Utilities;
using ECSTest.Systems.Roguelike;
using UI;
using UnityEngine;

public class GameInitialization : MonoBehaviour
{
    [SerializeField] private GameData gameData;
    [SerializeField] private Mission mission;

    [SerializeField] private bool isRoguelike = false;
    
    private Tower[] towers;
    
    private void Awake()
    {
        MusicManager.Clear();
        
#if UNITY_EDITOR
        GameObject hook = GameObject.Find("ReplayHook");
        if (hook == null)
#endif
        {
            GameServices.Instance.IsRoguelike = isRoguelike;
            
            if (isRoguelike)
            {
                towers = gameData.GetStartTowerForRoguelike();
                GameServices.Instance.CurrentMission = mission;
                var controller = this.gameObject.AddComponent<RoguelikeMainController>();
                controller.Init(gameData);
            }
            else if (MainMenuStarter.Instance == null)
            {
                towers = gameData.GetTowers();
                GameServices.Instance.CurrentMission = mission;
            }
            else
                towers = DataManager.Instance.GameData.GetTowersByUnlockManager();
            
            GameServices.Instance.InitMission(GameServices.Instance.CurrentMission,towers);
        }
#if UNITY_EDITOR
        else
            hook.GetComponent<ReplayHook>().InitMission();
#endif
        
    }

    private async void Start()
    {
        Messenger.Broadcast(GameEvents.TryStartMission,MessengerMode.DONT_REQUIRE_LISTENER);
        while (!MusicManager.IsReady)
        {
            await Awaitable.NextFrameAsync();
        }
        MusicManager.PlayBattleSceneBackground();
    }
}