using CardTD.Utilities;
using DG.Tweening;
using ECSTest.Components;
using ECSTest.Structs;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UI;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class TutorialManager : ScriptableObjSingleton<TutorialManager>
{
#if UNITY_EDITOR
    [Button]
    private void RestoreTutorialsInPrefs()
    {
        PlayerPrefs.SetString(nameof(TutorialsToShow), JsonUtility.ToJson(new TutorialsToShow(GetTutorialsToShowInitialList())));
        PlayerPrefs.Save();
    }
#endif  
    
    [SerializeField]
    private List<Tutorial> tutorials;
    
    private TutorialWindow tutorialWindow;
    private Transform uiManager;
    private EntityManager entityManager;

    private SceneEnum currentScene;

    private List<Tutorial> currentTutorials;
    private Tutorial showOnStartTutorial;
    [NonSerialized] public bool IsShowingTutorial;

    private TutorialsToShow tutorialsToShow = new(new List<string>());
    
    public bool HasCurrentTutorial(string eventName) => currentTutorials.Exists(x => x.EventName == eventName);
    private Tutorial getCurrentTutorial(string eventName) => currentTutorials.Find(x => x.EventName == eventName);
    public bool HasTutorialToShow(string eventName) => tutorialsToShow.Tutorials.Contains(eventName);

    public void Init(SceneEnum scene, Transform uiManager, int missionIndex, TutorialWindow window)
    {
        if(PlayerPrefs.HasKey(nameof(TutorialsToShow)))
            tutorialsToShow = JsonUtility.FromJson<TutorialsToShow>(PlayerPrefs.GetString(nameof(TutorialsToShow)));
        else
        {
            tutorialsToShow = new(GetTutorialsToShowInitialList());
            PlayerPrefs.SetString(nameof(TutorialsToShow), JsonUtility.ToJson(tutorialsToShow));
            PlayerPrefs.Save();
        }
        
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        tutorialWindow = window;
        this.uiManager = uiManager;
        currentScene = scene;
        showOnStartTutorial = null;
        IsShowingTutorial = false;

        currentTutorials = new();
        foreach (Tutorial tutorial in tutorials)
        {
            if (tutorial.Scene != scene) continue;

            if (tutorial.MissionIndex == missionIndex || tutorial.MissionIndex == -1)
            {
                if (HasTutorialToShow(tutorial.Key))
                {
                    currentTutorials.Add(tutorial);
                    if(tutorial.MissionIndex != -1 && tutorial.ShowOnStart)
                        showOnStartTutorial = tutorial;
                }
            }
        }
        
        ResolveConflicts();
        SubToEvents();
        EnableLocks();
    }

    public void Enable()
    {
        CheckNewAmmoTutorial(0);
        
        if (showOnStartTutorial is { Key: TutorialKeys.NewPart })
        {
            WeaponPart tutorialPart = DataManager.Instance.Get<PartsHolder>().Items.Find(x => x.SerializedID == "LightBarrel2");
            if(DataManager.Instance.GameData.Scrap < tutorialPart.ScrapCost)
                DataManager.Instance.GameData.AddScrap(tutorialPart.ScrapCost);
        }

        if (showOnStartTutorial != null && tutorialWindow != null)
        {
            if (showOnStartTutorial.Key == TutorialKeys.CoreDeactivation)
            {
                DOVirtual.DelayedCall(1, () =>
                {
                    ShowCoreDeactivationTutorial();
                    tutorialWindow.ShowTutorial(showOnStartTutorial);
                });
            }
            else
                tutorialWindow.ShowTutorial(showOnStartTutorial);
        }
    }

    public void ShowIsolatedTutorial(string key) => tutorialWindow.ShowTutorial(tutorials.Find(x=> x.Key == key));

    public void Dispose()
    {
        UnsubFromEvents();
    }

    private void SubToEvents()
    {
        //Mission 0
        if (HasCurrentTutorial(TutorialKeys.EnemyBehaviour))
            Messenger<int>.AddListener(GameEvents.NextWave, ShowEnemyBehaviourDialog);
        if (HasCurrentTutorial(TutorialKeys.BridgeShowcase_setup))
            Messenger<int>.AddListener(GameEvents.NextWave, ShowBridgeShowCaseSetup);
        if(HasCurrentTutorial(TutorialKeys.BridgeShowcase))
            Messenger<ChangePowerEvent>.AddListener(GameEvents.PowerChanged, ShowBridgeShowcase);
        //Mission 1
        if (HasCurrentTutorial(TutorialKeys.GameUpgrades))
            Messenger<Entity>.AddListener(GameEvents.BuildTower, ShowGameUpgrades);
        if(HasCurrentTutorial(TutorialKeys.UberTowerShowcase_setup))
            Messenger<int>.AddListener(GameEvents.NextWave, ShowUberTowerShowcaseSetup);
        if(HasCurrentTutorial(TutorialKeys.UberTowerShowcase))
            Messenger<ChangePowerEvent>.AddListener(GameEvents.PowerChanged, ShowUberTowerShowcase);
        //
        if(HasCurrentTutorial(TutorialKeys.MultipleWays))
            Messenger<int>.AddListener(GameEvents.NextWave, ShowMultipleWays);
        /*if(HasCurrentTutorial(TutorialKeys.CellDetachedTutor))
            Messenger<PowerCellEvent>.AddListener(GameEvents.CellDetached, ShowCellDetached);*/
        /*if(HasCurrentTutorial(TutorialKeys.NoCashForReload))
            Messenger<TowerVisual>.AddListener(TutorialKeys.NoCashForReload, ShowNoCashForReload);*/
        if(HasTutorialToShow(TutorialKeys.Directives))
            Messenger.AddListener(TutorialKeys.Directives, ShowDirectives);
        /*if(HasCurrentTutorial(TutorialKeys.NewAmmo))
            Messenger<AllEnums.TowerId>.AddListener(TutorialKeys.NewAmmo, CheckNewAmmoTutorial); */
        /*if(HasCurrentTutorial(TutorialKeys.DropZoneState))
            Messenger<DropZoneComponent, GridPositionComponent, bool>.AddListener(GameEvents.DropZoneStateChanged, ShowDropZoneState);*/
        /*if(HasCurrentTutorial($"{TutorialKeys.NewEnemy}_{AllEnums.FleshType.Mech}"))
           Messenger<int>.AddListener(GameEvents.NextWave, ShowNewEnemy);
        if(HasCurrentTutorial($"{TutorialKeys.NewEnemy}_{AllEnums.FleshType.Energy}"))
            Messenger<int>.AddListener(GameEvents.NextWave, ShowNewEnemy);*/
    }

    private void UnsubFromEvents()
    {
        Messenger<int>.RemoveListener(GameEvents.NextWave, ShowEnemyBehaviourDialog);
        Messenger<int>.RemoveListener(GameEvents.NextWave, ShowBridgeShowCaseSetup);
        Messenger<Entity>.RemoveListener(GameEvents.BuildTower, ShowGameUpgrades);
        Messenger<int>.RemoveListener(GameEvents.NextWave, ShowMultipleWays); 
        //Messenger<PowerCellEvent>.RemoveListener(GameEvents.CellDetached, ShowCellDetached);
        //Messenger<TowerVisual>.RemoveListener(TutorialKeys.NoCashForReload, ShowNoCashForReload);
        Messenger.RemoveListener(TutorialKeys.Directives, ShowDirectives);
        /*Messenger<AllEnums.TowerId>.RemoveListener(TutorialKeys.NewAmmo, CheckNewAmmoTutorial); */
        //Messenger<DropZoneComponent, GridPositionComponent, bool>.RemoveListener(GameEvents.DropZoneStateChanged, ShowDropZoneState);
        //Messenger<int>.RemoveListener(GameEvents.NextWave, ShowNewEnemy);
    }

    private void ResolveConflicts()
    {
        GameData gameData = DataManager.Instance.GameData;
        
        if (gameData.Stars.ContainsKey(0)) //1st mission completed
        {
            if (HasTutorialToShow(TutorialKeys.TowerBuilding)) 
                SetTutorialCompleted(TutorialKeys.TowerBuilding);
            if(HasTutorialToShow(TutorialKeys.EnemyBehaviour))
                SetTutorialCompleted(TutorialKeys.EnemyBehaviour);
            if(HasTutorialToShow(TutorialKeys.BridgeShowcase_setup))
                SetTutorialCompleted(TutorialKeys.BridgeShowcase_setup);
            if(HasTutorialToShow(TutorialKeys.BridgeShowcase))
                SetTutorialCompleted(TutorialKeys.BridgeShowcase);
        }
        
        if(gameData.Stars.ContainsKey(1) || (HasTutorialToShow(TutorialKeys.MenuUpgrades) && gameData.Factories.Find(x=>x.TowerId == AllEnums.TowerId.Light).Level >= 1)) //LightTower upgraded 
            SetTutorialCompleted(TutorialKeys.MenuUpgrades);
        
        if (HasTutorialToShow(TutorialKeys.Directives))
        {
            TowerFactory light = gameData.Factories.Find(x => x.TowerId == AllEnums.TowerId.Light);
            WeaponPart damageDirective = DataManager.Instance.Get<PartsHolder>().Directives.Find(x => x.SerializedID == "StarterDamageDirective");
            if (!gameData.Inventory.UnusedDirectives.ContainsKey(damageDirective) || light.Directives[0].WeaponPart != null) //DamageDirective set or LightTower directive slot[0] is occupied 
                SetTutorialCompleted(TutorialKeys.Directives);
        }
        
        if(HasTutorialToShow(TutorialKeys.BlockPath) && gameData.Stars.ContainsKey(3))
            SetTutorialCompleted(TutorialKeys.BlockPath);
        
        if (HasTutorialToShow(TutorialKeys.HardModeUnlocked) && gameData.HardStars.Count > 0) //Already completed hard missions
            SetTutorialCompleted(TutorialKeys.HardModeUnlocked);
        
        if(HasTutorialToShow(TutorialKeys.NewPart) && gameData.Stars.ContainsKey(16))
            SetTutorialCompleted(TutorialKeys.NewPart);
        
        if(HasTutorialToShow($"{TutorialKeys.NewEnemy}_{AllEnums.FleshType.Mech}") && gameData.Stars.ContainsKey(3))
           SetTutorialCompleted($"{TutorialKeys.NewEnemy}_{AllEnums.FleshType.Mech}");
        
        if(HasTutorialToShow($"{TutorialKeys.NewEnemy}_{AllEnums.FleshType.Energy}") && gameData.Stars.ContainsKey(6))
            SetTutorialCompleted($"{TutorialKeys.NewEnemy}_{AllEnums.FleshType.Energy}");
        
        if(HasTutorialToShow(TutorialKeys.Portals) && gameData.Stars.ContainsKey(19))
            SetTutorialCompleted(TutorialKeys.Portals);
        
        if(HasTutorialToShow(TutorialKeys.DropZoneActivation) && gameData.Stars.ContainsKey(35))
            SetTutorialCompleted(TutorialKeys.DropZoneActivation);
        
        if(HasTutorialToShow(TutorialKeys.CoreDeactivation) && gameData.Stars.ContainsKey(4))
            SetTutorialCompleted(TutorialKeys.CoreDeactivation);
    }
    
    public void SetTutorialCompleted(string tutorialKey)
    {
        if (tutorialsToShow.Tutorials.Contains(tutorialKey))
            tutorialsToShow.Tutorials.Remove(tutorialKey);
        if (currentTutorials.Exists(x => x.Key == tutorialKey))
            currentTutorials.Remove(getCurrentTutorial(tutorialKey));
        if (showOnStartTutorial != null && showOnStartTutorial.Key == tutorialKey)
            showOnStartTutorial = null;
        SaveTutorialsToShowToPrefs();
    }

    private void SaveTutorialsToShowToPrefs()
    {
        PlayerPrefs.SetString(nameof(TutorialsToShow), JsonUtility.ToJson(tutorialsToShow));
        PlayerPrefs.Save();
    }

    private void EnableLocks()
    {
        if (currentScene == SceneEnum.GameScene && HasTutorialToShow(TutorialKeys.GameUpgrades) && GameServices.Instance.CurrentMission.MissionIndex == 0)
            uiManager.Find("TowerInfoPanel").GetComponent<UIDocument>().rootVisualElement.Q<PriceButton>("SellButton").style.display = DisplayStyle.None;
        /*uiManager.Find("TowerInfoPanel").GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("TutorialLock").style.display = DisplayStyle.Flex;*/
    }

    #region Mission_0

    private void ShowEnemyBehaviourDialog(int waveNum)
    {
        if (waveNum == 1)
        {
            tutorialWindow.TutorialEndEvent += ShowEnemyBehaviourWindow;
            tutorialWindow.ShowTutorial(getCurrentTutorial(TutorialKeys.EnemyBehaviour_dialog));
            Messenger<int>.RemoveListener(GameEvents.NextWave, ShowEnemyBehaviourDialog);
        }
    }

    private void ShowEnemyBehaviourWindow()
    {
        tutorialWindow.TutorialEndEvent -= ShowEnemyBehaviourWindow;
        tutorialWindow.ShowTutorial(getCurrentTutorial(TutorialKeys.EnemyBehaviour));
    }

    private void ShowBridgeShowCaseSetup(int waveNum)
    {
        if (waveNum == 2)
        {
            tutorialWindow.ShowTutorial(getCurrentTutorial(TutorialKeys.BridgeShowcase_setup));
            Messenger<int>.RemoveListener(GameEvents.NextWave, ShowBridgeShowCaseSetup);
        }
    }

    private void ShowBridgeShowcase(ChangePowerEvent evt)
    {
        tutorialWindow.ShowTutorial(getCurrentTutorial(TutorialKeys.BridgeShowcase));
        Messenger<ChangePowerEvent>.RemoveListener(GameEvents.PowerChanged, ShowBridgeShowcase);
    }

#endregion

    #region Mission 1

    private void ShowGameUpgrades(Entity builtTower)
    {
        if(entityManager.HasComponent<Unclickable>(builtTower)) return;
        uiManager.Find("TowerInfoPanel").GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("TutorialLock").style.display = DisplayStyle.None;
           
        CashComponent cashComponent = GameServices.Instance.GetCashComponent();
        AttackerComponent attacker = entityManager.GetComponentData<AttackerComponent>(builtTower);
        PositionComponent position = entityManager.GetComponentData<PositionComponent>(builtTower);
            
        DataManager.Instance.Get<UpgradeProvider>().TryGetNextGameUpgrade(attacker.TowerType, attacker.Level, out CompoundUpgrade upgrade);

        if (!cashComponent.CanSpendCash(upgrade.Cost))
        {
            cashComponent.AddCash(upgrade.Cost);
            Entity cashUpdatedEvent = entityManager.CreateEntity();
            entityManager.SetName(cashUpdatedEvent, nameof(CashUpdatedEvent));
            entityManager.AddComponentData(cashUpdatedEvent, new CashUpdatedEvent() {CashAmount = upgrade.Cost, Position = position.Position});
        }

        Tutorial gameUpgradeTutorial = getCurrentTutorial(TutorialKeys.GameUpgrades);
        gameUpgradeTutorial.Stages[0].EnvGridPosition = new int2((int)position.Position.x - 1, (int)position.Position.y - 1);
        
        tutorialWindow.ShowTutorial(gameUpgradeTutorial);
        
        Messenger<Entity>.RemoveListener(GameEvents.BuildTower, ShowGameUpgrades);
    }

    private void ShowUberTowerShowcaseSetup(int waveNum)
    {
        if (waveNum == 3)
        {
            tutorialWindow.ShowTutorial(getCurrentTutorial(TutorialKeys.UberTowerShowcase_setup));
            Messenger<int>.RemoveListener(GameEvents.NextWave, ShowUberTowerShowcaseSetup);
        }
    }

    private void ShowUberTowerShowcase(ChangePowerEvent evt)
    {
        if (!entityManager.HasComponent<EntityHolderComponent>(evt.Entity))
            return;
        
        EntityHolderComponent holder = entityManager.GetComponentData<EntityHolderComponent>(evt.Entity);
        
        if (!entityManager.HasComponent<Unclickable>(holder.Entity)) return;
        tutorialWindow.ShowTutorial(getCurrentTutorial(TutorialKeys.UberTowerShowcase));
        Messenger<ChangePowerEvent>.RemoveListener(GameEvents.PowerChanged, ShowUberTowerShowcase);
    }


#endregion

#region Mission 5
    private void ShowMultipleWays(int waveNum)
    {
        if (waveNum == 0)
        {
            tutorialWindow.ShowTutorial(getCurrentTutorial(TutorialKeys.MultipleWays));
            Messenger<int>.RemoveListener(GameEvents.NextWave, ShowMultipleWays);
        }
    }

#endregion

    
    private void CheckNewAmmoTutorial(AllEnums.TowerId towerId)
    {
        if (currentScene == SceneEnum.Menu && HasCurrentTutorial(TutorialKeys.NewAmmo) && showOnStartTutorial == null)
        {
            foreach (WeaponPart part in DataManager.Instance.GameData.NewItems)
            {
                if (part.PartType == AllEnums.PartType.Ammo)
                {
                    if(towerId == 0 && DataManager.Instance.GameData.Stars.ContainsKey(2)) //on start ammo tutorial should appear only after mission 2
                        ShowNewAmmo(part, true);
                    else if(towerId != 0 && part.TowerId.HasFlag(towerId))
                        ShowNewAmmo(part, false);
                }
            }
        }
    }
    
    private void ShowNewAmmo(WeaponPart ammo, bool fromStart)
    {
        Tutorial ammoTutorial = getCurrentTutorial(TutorialKeys.NewAmmo).Clone();
        IEnumerable<AllEnums.TowerId> GetFlags(AllEnums.TowerId input)
        {
            foreach (AllEnums.TowerId value in Enum.GetValues(typeof(AllEnums.TowerId)))
            {
                if (input.HasFlag(value))
                {
                    yield return value;
                }
            }
        }

        AllEnums.TowerId firstTowerId = AllEnums.TowerId.Light;
        foreach (var towerId in GetFlags(ammo.TowerId))
        {
            firstTowerId = towerId;
            break;
        }
        uiManager.Find("WorkshopPanel").GetComponent<UIDocument>().rootVisualElement.Q<WorkshopPanel>().SetFactoryWidgetOnScrollStart(firstTowerId);
        ammoTutorial.Stages[2].UISelectionElementName = $"WorkshopFactoryWidget_{firstTowerId.ToString()}";
        ammoTutorial.Stages[5].UISelectionElementName = $"AmmoWidget_{ammo.SerializedID}";

        int skip = 0;
        if (!fromStart)
        {
            skip = 3;
            ammoTutorial.Stages.RemoveRange(0, skip);
            ammoTutorial.Stages[0].StageEvents = TutorialStageEvents.Dialog | TutorialStageEvents.UISelection | TutorialStageEvents.Delay;
        }
        
        tutorialWindow.ShowTutorial(ammoTutorial, skip);
        Messenger<AllEnums.TowerId>.RemoveListener(TutorialKeys.NewAmmo, CheckNewAmmoTutorial);
    }
    
    private void ShowDirectives()
    {
        Tutorial directivesTutorial = tutorials.Find(x=>x.Key == TutorialKeys.Directives).Clone();
        directivesTutorial.Stages.RemoveRange(0,4);
        tutorialWindow.ShowTutorial(directivesTutorial, 4);
        Messenger.RemoveListener(TutorialKeys.Directives, ShowDirectives);
    }

    private void ShowCellDetached(PowerCellEvent cellEvent)
    {
        Tutorial cellDetachedTutor = getCurrentTutorial(TutorialKeys.CellDetachedTutor);
        int2 position = entityManager.GetComponentData<GridPositionComponent>(cellEvent.Core).Value.GridPos;
        cellDetachedTutor.Stages[0].EnvGridPosition = position;
        tutorialWindow.ShowTutorial(cellDetachedTutor);
        Messenger<PowerCellEvent>.RemoveListener(GameEvents.CellDetached, ShowCellDetached);
    }

    private void ShowNoCashForReload(TowerVisual visual)
    {
        Tutorial noCashForReloadTutor = getCurrentTutorial(TutorialKeys.NoCashForReload);
        foreach (var stage in noCashForReloadTutor.Stages)
        {
            stage.EnvGridPosition = new int2((int)visual.transform.position.x - 1, (int)visual.transform.position.y - 1);
        }
        tutorialWindow.ShowTutorial(noCashForReloadTutor);
        Messenger<TowerVisual>.RemoveListener(TutorialKeys.NoCashForReload, ShowNoCashForReload);
    }

    private void ShowDropZoneState(DropZoneComponent dropZoneComponent, GridPositionComponent gridPos, bool turnedOn)
    {
        if(turnedOn) return;
        Tutorial dropZoneStateTutorial = getCurrentTutorial(TutorialKeys.DropZoneState);
        if (dropZoneStateTutorial != null)
        {
            dropZoneStateTutorial.Stages[0].EnvGridPosition = dropZoneStateTutorial.Stages[1].EnvGridPosition = gridPos.Value.GridPos;
            tutorialWindow.ShowTutorial(dropZoneStateTutorial);
        }
        Messenger<DropZoneComponent, GridPositionComponent, bool>.RemoveListener(GameEvents.DropZoneStateChanged, ShowDropZoneState);
    }

    private void ShowNewEnemy(int waveNum)
    {
        if (waveNum == 2 && GameServices.Instance.CurrentMission.MissionIndex == 2)
        {
            tutorialWindow.ShowTutorial(getCurrentTutorial($"{TutorialKeys.NewEnemy}_{AllEnums.FleshType.Mech.ToString()}"));
            Messenger<int>.RemoveListener(GameEvents.NextWave, ShowNewEnemy);
        }
        else if (waveNum == 4 && GameServices.Instance.CurrentMission.MissionIndex == 5)
        {
            tutorialWindow.ShowTutorial(getCurrentTutorial($"{TutorialKeys.NewEnemy}_{AllEnums.FleshType.Energy.ToString()}"));
            Messenger<int>.RemoveListener(GameEvents.NextWave, ShowNewEnemy);
        }
    }

    private void ShowCoreDeactivationTutorial()
    {
        var gate = GameServices.Instance.CurrentMission.Gates[0];
        var gateEntity = entityManager.CreateEntity();
        entityManager.SetName(gateEntity, "GateForTutorial");
        entityManager.AddComponentData(gateEntity, new EnvironmentVisualComponent(){});
        GridPositionStruct gridPos = new() { GridPos = gate.GridPos, GridSize = gate.GridSize };
        entityManager.AddComponentData(gateEntity, new GridPositionComponent() { Value = gridPos });
        tutorialWindow.ShowTutorial(showOnStartTutorial);
    }
    
    private List<string> GetTutorialsToShowInitialList()
    {
#if UNITY_EDITOR
        if (GameServices.Instance.SkipAllTutorials)
            return new();
#endif
        
        List<string> result = new();
        foreach (var tutorial in tutorials)
        {
            if(!string.IsNullOrEmpty(tutorial.EventName))
                result.Add(tutorial.EventName);
            else
                result.Add($"{tutorial.Scene}_{tutorial.MissionIndex.ToString()}");
        }

        return result;
    }
    
}

[Serializable]
public class Tutorial
{
    [FoldoutGroup("$Key")]public SceneEnum Scene;
    [FoldoutGroup("$Key")]public TutorialType TutorialType;
    [FoldoutGroup("$Key")]public int MissionIndex;
    [FoldoutGroup("$Key")]public bool ShowOnStart;
    [FoldoutGroup("$Key")]public string EventName;
    [FoldoutGroup("$Key")][ShowIf("TutorialType", TutorialType.Dialog)][TableList(ShowIndexLabels = true)] public List<TutorialStage> Stages;
    [FoldoutGroup("$Key")][ShowIf("TutorialType", TutorialType.Window)] public Texture2D Icon;
    
    [FoldoutGroup("$Key")][ShowInInspector]
    public string Key
    {
        get
        {
            if (TutorialType == TutorialType.Window)
                return EventName;
            return string.IsNullOrEmpty(EventName) ? $"{Scene}_{MissionIndex}" : $"{EventName}";
        }
    }

    public Tutorial Clone()
    {
        Tutorial clone = new Tutorial();
        clone.Scene = this.Scene;
        clone.TutorialType = this.TutorialType;
        clone.MissionIndex = this.MissionIndex;
        clone.ShowOnStart = this.ShowOnStart;
        clone.EventName = this.EventName;
        clone.Stages = new List<TutorialStage>();
        foreach (var stage in this.Stages)
        {
            clone.Stages.Add(stage.Clone());
        }
        clone.Icon = this.Icon;

        return clone;
    }
}

[Serializable]
public class TutorialStage
{
    public TutorialStageEvents StageEvents;

    [ShowIf(nameof(IsDialog))] public DialogLine DialogLine;
    
    [ShowIf(nameof(IsEnvSelection))] public string EnvPointerTextKey;
    [ShowIf(nameof(IsEnvSelection))] public int2 EnvGridPosition;
    [ShowIf(nameof(IsEnvSelection))] public bool IgnoreEnvironment;
    
    [ShowIf(nameof(IsUISelection))] public string UISelectionParentName;
    [ShowIf(nameof(IsUISelection))] public string UISelectionElementName;
    [ShowIf(nameof(IsUISelection))] public float2 TextFieldOffset;
    [ShowIf(nameof(HasAdditionalUISelection))] public string AdditionalUIHighlightParentName;
    [ShowIf(nameof(HasAdditionalUISelection))] public string AdditionalUIHighlightElementName;
    
    [ShowIf(nameof(HasDelay))] public float NextStageDelay;
    
    public bool IsDialog() => (StageEvents & TutorialStageEvents.Dialog) != 0;
    public bool IsEnvSelection() => (StageEvents & TutorialStageEvents.EnvironmentSelection) != 0;
    public bool IsUISelection() => (StageEvents & TutorialStageEvents.UISelection) != 0;
    public bool HasAdditionalUISelection () => (StageEvents & TutorialStageEvents.AdditionalUISelection) != 0;
    public bool IsAction() => (StageEvents & TutorialStageEvents.Action) != 0;
    public bool IsWindow() => (StageEvents & TutorialStageEvents.Window) != 0;
    public bool HasDelay() => (StageEvents & TutorialStageEvents.Delay) != 0;

    public TutorialStage Clone()
    {
        return new TutorialStage
        {
            StageEvents = this.StageEvents,
            DialogLine = this.DialogLine != null ? new DialogLine { CharacterKey = this.DialogLine.CharacterKey, CharacterPosition = this.DialogLine.CharacterPosition } : null,
            EnvPointerTextKey = this.EnvPointerTextKey,
            EnvGridPosition = this.EnvGridPosition,
            IgnoreEnvironment = this.IgnoreEnvironment,
            UISelectionParentName = this.UISelectionParentName,
            UISelectionElementName = this.UISelectionElementName,
            TextFieldOffset = this.TextFieldOffset,
            NextStageDelay = this.NextStageDelay
        };
    }
}

[Serializable]
public class TutorialsToShow
{
    public List<string> Tutorials;

    public TutorialsToShow(List<string> tutorials)
    {
        Tutorials = tutorials;
    }
}

[Flags]
public enum TutorialStageEvents
{
    Window = 1 << 0,
    Dialog = 1 << 1,
    EnvironmentSelection = 1 << 2,
    UISelection = 1 << 3,
    Action = 1 << 4,
    Delay = 1 << 5,
    AdditionalUISelection = 1 << 6
}

public enum TutorialType
{
    Window = 1,
    Dialog = 2
}

public enum SceneEnum
{
    GameScene,
    Menu
}