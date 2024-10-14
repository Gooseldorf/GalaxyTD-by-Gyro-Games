using System;
using System.Collections.Generic;
using DefaultNamespace.Systems.Interfaces;
using ECSTest.Components;
using ECSTest.Systems;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.SceneManagement;
using UnityEngine;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections;
using CardTD.Utilities;
using DefaultNamespace;
using DG.Tweening;
using ECSTest.Systems.Roguelike;
using Systems.Attakers;
using Unity.Core;
using Random = Unity.Mathematics.Random;
using static AllEnums;

public class GameServices : ScriptableObjSingleton<GameServices>
{
    private const float sellTowerTime = 4f;
    public const float DropZoneReactivateDropZone = 30f;
    public MovementSettingsSO MovementSettingsSo;
    [SerializeField] private List<ScriptableObject> systems;

    public RenderDataHolder RenderDataHolder;
    [ShowInInspector] public Mission CurrentMission { get; set; }
    public bool IsHard;
    public bool SkipAllDialogs = false;
    public bool SkipAllTutorials = false;
    public float CurrentTime => GetCurrentTime();

    private float timeSinceLastUpdate = 0;

    [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
    private Tower[] towers;

    private List<IUpdatable> updatables = new();

    private float fixedDeltaTime = 1f / 60f;

    private World missionBackUpWorld;
    private World systemHandlesBackUpWorld; //TODO: Remove when we'll get rid of systemHandles

    private int timeStoppedCounter = 0;
    private int saveTimeStopped = 0;
    private float timeScale = 1;

    [SerializeField] private CritterStats critterStats;

    public float CurrentTimeScale => timeScale;


    public IReadOnlyList<Tower> Towers => towers;

    public bool IsRoguelike { get; set; }

    public Reward CurrentReward;

    public bool UseBulletCost = true;

    public Tower GetTower(TowerId towerType, Entity towerEntity)
    {
        if (IsRoguelike)
            return RoguelikeMainController.Link.GetTower(towerEntity);
        foreach (Tower tower in towers)
            if (tower.TowerId == towerType)
                return tower;
        return null;
    }

    public void AddTower(Tower tower)
    {
        List<Tower> list = new (towers) {tower};
        towers = list.ToArray();
    }

    [Button]
    public void SimulateSpike()
    {
        TimeData time = World.DefaultGameObjectInjectionWorld.Time;
        World.DefaultGameObjectInjectionWorld.PushTime(new TimeData(time.ElapsedTime + 3, 3));
    }

    public void UpdateTime(float deltaTime)
    {
        if (this.CurrentMission == null)
            return;

        try
        {
            timeSinceLastUpdate += deltaTime;

            while (timeSinceLastUpdate > fixedDeltaTime)
            {
                timeSinceLastUpdate -= fixedDeltaTime;
                Ticks++;
                for (int i = 0; i < updatables.Count; i++)
                {
                    try
                    {
                        if (updatables[i].IsEnabled)
                            updatables[i].Tick(fixedDeltaTime);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }


    public T Get<T>()
    {
        for (int i = 0, count = this.systems.Count; i < count; i++)
        {
            if (this.systems[i] is T result)
            {
                return result;
            }
        }
        throw new Exception($"Element of type {typeof(T).Name} is not found!");
    }

    public int Ticks { get; private set; }


    public void SetTowers(Tower[] newTowers)
    {
        towers = newTowers;
    }

    public void InitMission(Mission mission, Tower[] initTowers)
    {
        UseBulletCost = DataManager.Instance.GameData.UseBulletCost;
        
        World world = World.DefaultGameObjectInjectionWorld;

        world.SetTime(new TimeData(world.Time.ElapsedTime, 0));

        ToggleSystems(true);

        Get<SimpleEffectManager>().Init();
        ReplayManager.Instance.LogMissionInit(mission, initTowers);
        SetTowers(initTowers);
        CurrentMission = mission;

        world.EntityManager.CreateSingleton(new CashComponent(mission.CashPerWaveStart));

        CreateRandom(world, 100);

        FixedStepSimulationSystemGroup mainGroup = world.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();

        FixedRateCatchUpManager fixedRateCatchUpManager = new(1.0f / 60);
        //TODO : new rate manager - TimeUtils get new rate manager - проверить н алаги

        mainGroup.RateManager = fixedRateCatchUpManager;
        mainGroup.World.MaximumDeltaTime = 0.09f;
        fixedRateCatchUpManager.SetTime(world.Time.ElapsedTime);

        MapCreator mapCreator = Get<MapCreator>();

        Cell[,] cells = mission.CreateCells();

        //TODO : refactor float variables - (left....)
        float2 botLeftCorner = new float2(-1, -1);
        float2 topRightCorner = new float2(cells.GetLength(0) + 1, cells.GetLength(1) + 1);

        CreepsLocatorSystem.Init(world, botLeftCorner, topRightCorner, cells.GetLength(0), cells.GetLength(1));
        CreepsCacheBuildSystem.Init(world, botLeftCorner, topRightCorner);
        ObstaclesLocatorSystem.Init(world, botLeftCorner, topRightCorner);
        ObstaclesCacheBuildSystem.Init(world, botLeftCorner, topRightCorner);
        CritterSystem.Init(CurrentMission, cells, world, critterStats);
        ProjectileSystemBase.Init(world, botLeftCorner, topRightCorner);
        world.Unmanaged.GetExistingSystemState<WinLoseSystem>().Enabled = true;

        HashSet<CreepStats> uniqCreepStats = SpawnerSystem.Init(CurrentMission, world);
        DynamicSpawnerSystem.Init(world, mission, uniqCreepStats);

        TouchCamera.Instance.Init(cells.GetLength(0), cells.GetLength(1));
        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<BRGSystem>().Init(uniqCreepStats);
        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextVisualizationSystem>().Init();

        timeStoppedCounter = 0;
        SetTimeScale(1);

        mapCreator.CreateMapFromMission(CurrentMission);

        updatables.Clear();
        foreach (ScriptableObject system in systems)
        {
            if (system is IInitializable initializable)
                initializable.Init();

            if (system is IUpdatable updatable)
            {
                updatables.Add(updatable);
                updatable.IsEnabled = true;
            }
        }

        CurrentMission.InstantiateObjects(out NativeParallelMultiHashMap<Entity, Entity> connectedPowerables);
        FlowFieldBuildCacheSystem.Init(world, cells);
        PowerSystemBase.Init(world, ref connectedPowerables);

        SaveBackUpWorld();
        //SaveWorld1(botLeftCorner, topRightCorner);
        
        Messenger.Broadcast(GameEvents.InitGame,MessengerMode.DONT_REQUIRE_LISTENER);
        
        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TileDecalSystem>().Init(cells);
    }

    private void CreateRandom(World world, uint seed)
    {
        NativeArray<Random> randoms = new (JobsUtility.MaxJobThreadCount, Allocator.Persistent);

        for (int i = 0; i < randoms.Length; i++)
        {
            randoms[i] = new Unity.Mathematics.Random((uint)(seed + i));
        }

        world.EntityManager.CreateSingleton(new RandomComponent() { Randoms = randoms });
    }

    public void SetPause(bool pause)
    {
        if (pause)
            timeStoppedCounter++;
        else
            timeStoppedCounter--;


        if (timeStoppedCounter > 0 && saveTimeStopped < 0)
        {
            saveTimeStopped += timeStoppedCounter;
            timeStoppedCounter = saveTimeStopped;
        }

        if (timeStoppedCounter < 0)
        {
            saveTimeStopped = -timeStoppedCounter;
            timeStoppedCounter = 0;
        }

        SetTimeScale(this.timeScale);

        MusicManager.MuteOnPauseEvent(pause);
    }


    public void SetTimeScale(float scale)
    {
        timeScale = scale;
        FixedStepSimulationSystemGroup sim = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
        
        if (timeStoppedCounter <= 0)
        {
            Time.timeScale = scale;
            if (!sim.Enabled)
                sim.Enabled = true;
        }
        else
        {
            Time.timeScale = 0;
            if (sim.Enabled)
                sim.Enabled = false;
        }
    }

    public void Restart()
    {
        MusicManager.StopAllSounds();
        MusicManager.PlayBattleSceneBackground();

        World activeWorld = World.DefaultGameObjectInjectionWorld;

        Get<SimpleEffectManager>().ClearStaticEnvironments();

        DestroyWorld(activeWorld, false);

        LoadWorld(missionBackUpWorld, activeWorld);

        timeStoppedCounter = 0;
        SetTimeScale(1);
        activeWorld.Unmanaged.GetExistingSystemState<WinLoseSystem>().Enabled = true;

        TouchCamera.Instance.Reset();

        Messenger.Broadcast(GameEvents.Restart, MessengerMode.DONT_REQUIRE_LISTENER);
    }

    public void SaveSystemHandlesBackUp()
    {
        systemHandlesBackUpWorld ??= SaveWorld(false);
    } //TODO: Remove when we'll get rid of systemHandles

    public void ReturnToMenu()
    {
        var activeWorld = World.DefaultGameObjectInjectionWorld;

        foreach (ScriptableObject system in systems)
        {
            if (system is IInitializable initializable)
            {
                initializable.DeInit();
                initializable.Clear();
            }
        }

        Get<SimpleEffectManager>().ClearPools();
        DynamicSpawnerSystem.Dispose(World.DefaultGameObjectInjectionWorld);
        activeWorld.GetExistingSystemManaged<BRGSystem>().Clear();
        activeWorld.GetExistingSystemManaged<TextVisualizationSystem>().Clear();
        DestroyWorld(activeWorld, true);
        activeWorld.EntityManager.CopyAndReplaceEntitiesFrom(systemHandlesBackUpWorld.EntityManager); //TODO: Remove when we'll get rid of systemHandles
        missionBackUpWorld.Dispose();
        ToggleSystems(false);
        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TileDecalSystem>().Dispose();
        DOTween.KillAll();
        LoadingScreen.Instance.Show(() =>
        {
            SceneManager.LoadScene(0, LoadSceneMode.Single);
        });
    }

    private void SaveBackUpWorld() => missionBackUpWorld = SaveWorld(true);

    private World SaveWorld(bool withSingletons)
    {
        World activeWorld = World.DefaultGameObjectInjectionWorld;
        var world = new World("lockStepWorld", WorldFlags.Simulation);
        world.EntityManager.CopyAndReplaceEntitiesFrom(activeWorld.EntityManager);
        world.SetTime(new Unity.Core.TimeData(activeWorld.Time.ElapsedTime, 0));
        if (withSingletons)
            WorldSaver.SaveSingletons(activeWorld.EntityManager);
        return world;
    }

    private void LoadWorld(World worldToLoad, World activeWorld)
    {
        activeWorld.EntityManager.CopyAndReplaceEntitiesFrom(worldToLoad.EntityManager);
        activeWorld.SetTime(new Unity.Core.TimeData(worldToLoad.Time.ElapsedTime, 0));
        FixedStepSimulationSystemGroup mainGroup = activeWorld.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
        (mainGroup.RateManager as FixedRateCatchUpManager).SetTime(worldToLoad.Time.ElapsedTime);
        WorldSaver.LoadSingletons(activeWorld.EntityManager);
        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TileDecalSystem>().ClearTileDecals();
    }

    private void DestroyWorld(World activeWorld, bool disposeSingletons)
    {
        activeWorld.EntityManager.CompleteAllTrackedJobs();

        activeWorld.GetExistingSystemManaged<BeginFixedStepSimulationEntityCommandBufferSystem>().Update();
        activeWorld.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>().Update();

        if (disposeSingletons)
            WorldSaver.Dispose(activeWorld.EntityManager);
        activeWorld.EntityManager.DestroyEntity(activeWorld.EntityManager.UniversalQuery);

        activeWorld.GetExistingSystemManaged<CompanionGameObjectUpdateTransform2DSystem>().Update();

        //OnCollisionEventSystem.ClearEvents();
        activeWorld.EntityManager.DestroyAndResetAllEntities();
        SpawnZoneVisualizator.DisposeArrays();
    }

    public void IncreaseReward()
    {
       CurrentReward =  DataManager.Instance.GameData.IncreaseReward(CurrentReward);
    }

    public void ToggleSystems(bool isActive)
    {
        foreach (var sys in World.DefaultGameObjectInjectionWorld.Systems)
        {
            sys.Enabled = isActive;
        }
    }

    private Entity SelectFreeDropZone(float3 position)
    {
        EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery dropZonesQuery = manager.CreateEntityQuery(new ComponentType[]
        {
            typeof(DropZoneComponent), typeof(GridPositionComponent), typeof(PositionComponent), typeof(DestroyComponent), typeof(PowerableComponent)
        });
        if (dropZonesQuery.IsEmpty)
            return Entity.Null;

        if (GetEntityInPosition(position, dropZonesQuery, out Entity result, out int index))
        {
            using NativeArray<PowerableComponent> powerables = dropZonesQuery.ToComponentDataArray<PowerableComponent>(Allocator.Temp);
            if (!powerables[index].IsTurnedOn)
            {
                return Entity.Null;
            }

            using NativeArray<DropZoneComponent> dropZones = dropZonesQuery.ToComponentDataArray<DropZoneComponent>(Allocator.Temp);
            if (dropZones[index].IsOccupied || !dropZones[index].IsPossibleToBuild)
            {
                return Entity.Null;
            }
        }

        //if (result != Entity.Null || manager.GetComponentData<DropZoneComponent>(dropZone).IsOccupied)
        //    return Entity.Null;

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="position"></param>
    /// <returns>Can return null</returns>
    private Entity SelectTower(float3 position)
    {
        EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

        EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<AttackerComponent, GridPositionComponent, PositionComponent, DestroyComponent>()
            .WithAbsent<Unclickable>()
            .Build(manager);

        if (query.IsEmpty)
            return Entity.Null;

        GetEntityInPosition(position, query, out Entity result, out int index);

        return result;
    }

    private static bool GetEntityInPosition(float3 position, EntityQuery query, out Entity result, out int index)
    {
        result = Entity.Null;
        bool entityFound = false;
        index = -1;

        NativeArray<GridPositionComponent> gridPositionComponents = query.ToComponentDataArray<GridPositionComponent>(Allocator.Temp);
        NativeArray<DestroyComponent> destroyComponents = query.ToComponentDataArray<DestroyComponent>(Allocator.Temp);
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

        float2 dopPoint;
        for (int i = 0; i < entities.Length; i++)
        {
            if (destroyComponents[i].IsNeedToDestroy)
                continue;

            dopPoint = gridPositionComponents[i].Value.GridPos + gridPositionComponents[i].Value.GridSize;

            if (position.x < dopPoint.x && position.x > gridPositionComponents[i].Value.GridPos.x && position.y < dopPoint.y && position.y > gridPositionComponents[i].Value.GridPos.y)
            {
                result = entities[i];
                index = i;
                entityFound = true;
                break;
            }
        }

        gridPositionComponents.Dispose();
        destroyComponents.Dispose();
        entities.Dispose();

        return entityFound;
    }

    public bool BuildTower(Tower towerPrototype, Entity dropZone, bool preinstalled = false)
    {
        EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        ReplayManager.Instance.LogBuildTower(towerPrototype, dropZone);

        Entity towerEntity = manager.CreateEntity();
        DropZoneComponent dropZoneComponent = SetDropZoneOccupied(ref manager, dropZone, towerEntity);
        manager.SetName(towerEntity, $"{towerPrototype.TowerId}");

        PositionComponent positionComponent = manager.GetComponentData<PositionComponent>(dropZone);
        manager.AddComponentData(towerEntity, positionComponent);

        GridPositionComponent dropZoneGrid = manager.GetComponentData<GridPositionComponent>(dropZone);

        Messenger<DropZoneComponent, GridPositionComponent, bool>.Broadcast(GameEvents.DropZoneStateChanged, dropZoneComponent, dropZoneGrid, true, MessengerMode.DONT_REQUIRE_LISTENER);

        manager.AddComponentData(towerEntity, dropZoneGrid);

        manager.AddComponentData(towerEntity, manager.GetComponentData<Identifiable>(dropZone));


        var attacker = new AttackerComponent(towerPrototype);

        if (!UseBulletCost)
            attacker.AttackStats.ReloadStats.BulletCost = 0;

        manager.AddComponentData(towerEntity, new AttackerStatisticComponent() { Kills = 0, Shoots = 0, CashBuildSpent = towerPrototype.BuildCost });

        DamageModifiers damageModifiersComponent = towerPrototype.DamageModifiers;
        manager.AddComponentData(towerEntity, damageModifiersComponent);

        manager.AddBuffer<BuffBuffer>(towerEntity);

        switch (towerPrototype.AttackStats)
        {
            case GunStats gunStats:
                manager.AddComponentData(towerEntity, new GunStatsComponent(gunStats));
                break;
            case RocketStats rocketStats:
                manager.AddComponentData(towerEntity, new RocketStatsComponent(rocketStats));
                break;
            case MortarStats mortarStats:
                manager.AddComponentData(towerEntity, new MortarStatsComponent(mortarStats));
                break;
        }

        manager.AddComponentData(towerEntity, new DestroyComponent { IsNeedToDestroy = false });
        manager.AddComponentData(towerEntity,
            new CostComponent { Cost = towerPrototype.BuildCost, SellModifier = towerPrototype.AttackStats.Sellmodifier, CostMultiplier = towerPrototype.CostMultiplier });
        manager.AddComponentData(towerEntity, new EntityHolderComponent { Entity = dropZone });


        bool isTurnedOnByDefault = true;

        if (preinstalled)
        {
            manager.AddComponentData(towerEntity, new Unclickable());
            var powerableComponent = manager.GetComponentData<PowerableComponent>(dropZone);

            if (!powerableComponent.IsTurnedOn)
            {
                isTurnedOnByDefault = false;
                attacker.AttackPattern = AttackPattern.Off;
                Messenger<Entity>.Broadcast(GameEvents.TowerUpdated, towerEntity, MessengerMode.DONT_REQUIRE_LISTENER);
            }
        }

        manager.AddComponentData(towerEntity, new PowerableComponent(isTurnedOnByDefault, false));
        manager.AddComponentData(towerEntity, attacker);

        TagsComponent tagsComponent = new() { Tags = new List<Tag>(), };
        foreach (ISlot directive in towerPrototype.Directives)
        {
            if (directive.WeaponPart == null) continue;

            foreach (Tag tag in directive.WeaponPart.Bonuses)
            {
                if (tag is IStaticTag) continue;
                tagsComponent.Tags.Add(tag);
            }
        }

        if (towerPrototype.Ammo.WeaponPart != null)
            foreach (Tag tag in towerPrototype.Ammo.WeaponPart.Bonuses)
            {
                if (tag is IStaticTag) continue;
                tagsComponent.Tags.Add(tag);
            }

        manager.AddComponentData(towerEntity, tagsComponent);

        if (!tagsComponent.Tags.Find(x => x is NoObstacleTag))
            CreateObstacle(ref manager, dropZoneGrid, towerEntity);

        if (!preinstalled)
        {
            RefRW<CashComponent> cashComponent = GetCashComponentRW();
            cashComponent.ValueRW.BuildTower(towerPrototype);
            CashComponent.SpawnCashUpdatedEvent(manager, -towerPrototype.BuildCost, positionComponent.Position);
        }

        RefRW<BaseFlowField> cellsHolder = GetBaseFlowField();
        cellsHolder.ValueRW.ChangeFlowField(dropZoneGrid.Value, true);
        CreateBaseFieldUpdateEvent();
        
        Messenger<Entity>.Broadcast(GameEvents.BuildTower, towerEntity, MessengerMode.DONT_REQUIRE_LISTENER);

        if (!preinstalled)
        {
            Messenger<string>.Broadcast(GameEvents.BuildTowerTowerId, $"{towerPrototype.TowerId}", MessengerMode.DONT_REQUIRE_LISTENER);
            Messenger<Entity>.Broadcast(UIEvents.ObjectSelected, towerEntity, MessengerMode.DONT_REQUIRE_LISTENER);
        }

        return true;
    }

    public void CreateObstacle(ref EntityManager manager, GridPositionComponent dropZoneGrid, Entity entity)
    {
        float2 startPosition = BaseFlowField.GetCenterPosition(dropZoneGrid.Value.GridPos, new int2(1, 1)).xy + new float2(-0.5f, -0.5f);
        SquareObstacle towerSquareObstacle = new();
        float2 leftPoint = startPosition + dropZoneGrid.Value.GridSize;
        towerSquareObstacle.ObstacleType = ObstacleType.OnlyRicochet;
        towerSquareObstacle.BotLeftPoint = startPosition;
        towerSquareObstacle.TopLeftPoint = new float2(startPosition.x, leftPoint.y);
        towerSquareObstacle.TopRightPoint = leftPoint;
        towerSquareObstacle.BotRightPoint = new float2(leftPoint.x, startPosition.y);

        if (manager.HasComponent<SquareObstacle>(entity))
            manager.SetComponentData(entity, towerSquareObstacle);
        else
            manager.AddComponentData(entity, towerSquareObstacle);
    }

    public void SkipTimeToNextWave()
    {
        RefRW<TimeSkipper> timeSkipper = GetTimeSkipper(out float elapsedTime);
        if (timeSkipper.ValueRO.CanSkip(elapsedTime))
            timeSkipper.ValueRW.SkipTime(elapsedTime);
    }

    public void SkipFirstWaveOffset()
    {
        ReplayManager.Instance.LogStartClick();
        RefRW<TimeSkipper> timeSkipper = GetTimeSkipper(out float elapsedTime);
        timeSkipper.ValueRW.SkipFirstWaveOffset(elapsedTime);
    }

    [Button]
    public void SkipTime(float time)
    {
        RefRW<TimeSkipper> timeSkipper = GetTimeSkipper(out float elapsedTime);
        Debug.Log($"time {time} elapsed time: {GetCurrentTime()}");
        timeSkipper.ValueRW.TimeSkip(time);
        Debug.Log($"time {time} elapsed time: {GetCurrentTime()}");
    }

    private float GetCurrentTime()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entityManager.CompleteDependencyBeforeRO<TimeSkipper>();

        EntityQuery skipperQuery = entityManager.CreateEntityQuery(new ComponentType[] { typeof(TimeSkipper) });
        float elapsedTime = (float)World.DefaultGameObjectInjectionWorld.Time.ElapsedTime;

        if (skipperQuery.IsEmpty)
        {
            // Debug.LogWarning("skipperQuery is empty");
            return 0;
        }

        skipperQuery.TryGetSingleton(out TimeSkipper timeSkipper);

        return timeSkipper.CurrentTime(elapsedTime);
    }

    public int CurrentWave()
    {
        RefRW<TimeSkipper> timeSkipper = GetTimeSkipper(out float elapsedTime);
        return timeSkipper.ValueRO.CurrentWave(elapsedTime);
    }

    public float TimeForNextWave()
    {
        RefRW<TimeSkipper> timeSkipper = GetTimeSkipper(out float elapsedTime);
        return timeSkipper.ValueRO.TimeToNextWave(elapsedTime);
    }

    public bool CanSkip()
    {
        RefRW<TimeSkipper> timeSkipper = GetTimeSkipper(out float elapsedTime);
        return timeSkipper.ValueRO.CanSkip(elapsedTime);
    }

    public RefRW<BaseFlowField> GetBaseFlowField()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entityManager.CompleteDependencyBeforeRW<BaseFlowField>();

        EntityQuery baseFieldQuery = entityManager.CreateEntityQuery(new ComponentType[] { typeof(BaseFlowField) });
        return baseFieldQuery.GetSingletonRW<BaseFlowField>();
    }

    private RefRW<TimeSkipper> GetTimeSkipper(out float elapsedTime)
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entityManager.CompleteDependencyBeforeRW<TimeSkipper>();

        EntityQuery skipperQuery = entityManager.CreateEntityQuery(new ComponentType[] { typeof(TimeSkipper) });
        elapsedTime = (float)World.DefaultGameObjectInjectionWorld.Time.ElapsedTime;

        if (skipperQuery.IsEmpty)
        {
            Debug.LogWarning("skipperQuery is empty");
            return default;
        }

        return skipperQuery.GetSingletonRW<TimeSkipper>();
    }

    public CashComponent GetCashComponent()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        return GetCashComponent(entityManager);
    }

    public CashComponent GetCashComponent(EntityManager manager)
    {
        manager.CompleteDependencyBeforeRW<CashComponent>();
        EntityQuery cashQuery = manager.CreateEntityQuery(new ComponentType[] { typeof(CashComponent) });
        return cashQuery.GetSingleton<CashComponent>();
    }

    private RefRW<CashComponent> GetCashComponentRW()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entityManager.CompleteDependencyBeforeRW<CashComponent>();
        EntityQuery cashQuery = entityManager.CreateEntityQuery(new ComponentType[] { typeof(CashComponent) });
        return cashQuery.GetSingletonRW<CashComponent>();
    }

    [Button]
    public void WinMission(int powerCellsLeft = 1000)
    {
        int stars = CalculateStars(powerCellsLeft, out int powerCellsMax);
        Messenger<int, int>.Broadcast(GameEvents.Win, stars, powerCellsMax, MessengerMode.DONT_REQUIRE_LISTENER);

        CurrentReward = DataManager.Instance.GameData.WinMission(CurrentMission, stars, IsHard);
    }

    [Button]
    public void LoseMission()
    {
        //Disable all Systems
        Messenger.Broadcast(GameEvents.Lost);
    }

    private int CalculateStars(int powerCellsLeft, out int powerCellsMax)
    {
        powerCellsMax = 0;
        foreach (var energyCore in CurrentMission.EnergyCores)
            powerCellsMax += energyCore.PowerCellCount;

        float percentagePowerCellsLeft = (float)powerCellsLeft / powerCellsMax;

        int starCount = percentagePowerCellsLeft switch
        {
            >= 0.66f => 3,
            >= 0.33f => 2,
            _ => 1
        };

        return starCount;
    }

    #region Drop zone and towers

    public bool ManualReload(Entity tower)
    {
        EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

        var cashComponent = GetCashComponentRW();

        var attacker = manager.GetComponentData<AttackerComponent>(tower);

        int cost = attacker.AttackStats.ReloadStats.ManualReloadCost(attacker.Bullets);

        if (!cashComponent.ValueRO.CanSpendCash(cost)) return false;

        ReloadingSystem.Reload(ref attacker);

        cashComponent.ValueRW.SpendCash(cost);
        manager.SetComponentData(tower, attacker);

        var positionComponent = manager.GetComponentData<PositionComponent>(tower);
        CashComponent.SpawnCashUpdatedEvent(manager, -cost, positionComponent.Position);

        ReplayManager.Instance.LogManualReload(tower);

        Entity reloadEvent = manager.CreateEntity();
        manager.SetName(reloadEvent, nameof(ReloadEvent));
        manager.AddComponentData(reloadEvent, new ReloadEvent() { Tower = tower });

        return true;
    }

    public Entity Select(float3 position)
    {
        var tower = SelectTower(position);
        if (tower == Entity.Null)
        {
            var dropZone = SelectFreeDropZone(position);
            return dropZone;
        }

        return tower;
    }

    public void UpdateTowerLevelData(Entity towerEntity, int level, Tower towerPrototype)
    {
        EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

        manager.SetComponentData(towerEntity, towerPrototype.DamageModifiers);

        switch (towerPrototype.AttackStats)
        {
            case GunStats gunStats:
                manager.SetComponentData(towerEntity, new GunStatsComponent(gunStats));
                break;
            case RocketStats rocketStats:
                manager.SetComponentData(towerEntity, new RocketStatsComponent(rocketStats));
                break;
            case MortarStats mortarStats:
                manager.SetComponentData(towerEntity, new MortarStatsComponent(mortarStats));
                break;
        }

        manager.SetComponentData(towerEntity,
            new CostComponent { Cost = towerPrototype.BuildCost, SellModifier = towerPrototype.AttackStats.Sellmodifier, CostMultiplier = towerPrototype.CostMultiplier });

        TagsComponent tagsComponent = new() { Tags = new List<Tag>(), };
        foreach (ISlot directive in towerPrototype.Directives)
        {
            if (directive.WeaponPart == null) continue;

            foreach (Tag tag in directive.WeaponPart.Bonuses)
            {
                if (tag is IStaticTag) continue;
                tagsComponent.Tags.Add(tag);
            }
        }

        if (towerPrototype.Ammo.WeaponPart != null)
            foreach (Tag tag in towerPrototype.Ammo.WeaponPart.Bonuses)
            {
                if (tag is IStaticTag) continue;
                tagsComponent.Tags.Add(tag);
            }

        manager.SetComponentData(towerEntity, tagsComponent);

        manager.SetComponentData(towerEntity, new AttackerComponent(towerPrototype));


        for (int i = 0; i < level; i++)
        {
            AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(towerEntity);
            attackerComponent.Level++;
            if (CanTowerUpgrade(attackerComponent, out CompoundUpgrade nextGameUpgrade))
                nextGameUpgrade.ApplyUpgrades(towerEntity, ref manager, ref attackerComponent);
        }
    }

    public bool UpgradeTower(Entity towerEntity)
    {
        EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(towerEntity);
        AttackerStatisticComponent statisticComponent = manager.GetComponentData<AttackerStatisticComponent>(towerEntity);
        CostComponent costComponent = manager.GetComponentData<CostComponent>(towerEntity);

        if (!CanTowerUpgrade(attackerComponent, out CompoundUpgrade nextGameUpgrade))
            return false;

        var cashComponent = GetCashComponentRW();

        int upgradeCost = Mathf.RoundToInt(nextGameUpgrade.Cost * costComponent.CostMultiplier);

        if (!cashComponent.ValueRO.CanSpendCash(upgradeCost))
            return false;

        ReplayManager.Instance.LogUpgradeTower(towerEntity);
        cashComponent.ValueRW.SpendCash(upgradeCost);
        attackerComponent.Level++;

        var positionComponent = manager.GetComponentData<PositionComponent>(towerEntity);
        CashComponent.SpawnCashUpdatedEvent(manager, -upgradeCost, positionComponent.Position);

        statisticComponent.CashUpgradeSpent += upgradeCost;
        costComponent.Cost += upgradeCost;

        manager.SetComponentData(towerEntity, statisticComponent);
        manager.SetComponentData(towerEntity, costComponent);

        nextGameUpgrade.ApplyUpgrades(towerEntity, ref manager, ref attackerComponent);

        Messenger<Entity>.Broadcast(GameEvents.TowerUpdated, towerEntity, MessengerMode.DONT_REQUIRE_LISTENER);
        return true;
    }

    public bool CanTowerUpgrade(AttackerComponent attacker, out CompoundUpgrade nextGameUpgrade)
    {
        //Debug.Log("attacker.Level = " + attacker.Level);
        var upgradeProvider = DataManager.Instance.Get<UpgradeProvider>();
        if (attacker.Level >= upgradeProvider.GameUpgradeLevelCap)
        {
            nextGameUpgrade = null;
            return false;
        }

        return upgradeProvider.TryGetNextGameUpgrade(attacker.TowerType, attacker.Level, out nextGameUpgrade);
    }

    public bool ChangeFiringModel(Entity tower)
    {
        ReplayManager.Instance.LogChangeFiringMode(tower);
        if (tower == Entity.Null) return false;

        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;

        var attackComponent = manager.GetComponentData<AttackerComponent>(tower);

        attackComponent.AttackPattern = attackComponent.AttackStats.ShootingStats.GetNextAvailableAttackPattern(attackComponent.AttackPattern);

        manager.SetComponentData(tower, attackComponent);
        Messenger<Entity>.Broadcast(GameEvents.TowerUpdated, tower, MessengerMode.DONT_REQUIRE_LISTENER);
        return true;
    }

    public void ToggleTower(Entity tower)
    {
        if (tower == Entity.Null) return;

        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var attackComponent = manager.GetComponentData<AttackerComponent>(tower);

        if (attackComponent.AttackPattern != AttackPattern.Off)
        {
            attackComponent.AttackPattern = AttackPattern.Off;
        }
        else
            attackComponent.AttackPattern = attackComponent.AttackStats.ShootingStats.GetNextAvailableAttackPattern(attackComponent.AttackPattern);

        manager.SetComponentData(tower, attackComponent);
        Messenger<Entity>.Broadcast(GameEvents.TowerUpdated, tower, MessengerMode.DONT_REQUIRE_LISTENER);
    }

    public bool SellTower(Entity tower)
    {
        ReplayManager.Instance.LogSellTower(tower);
        if (tower == Entity.Null)
            return false;

        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;

        manager.SetComponentEnabled<AttackerComponent>(tower, false);
        manager.SetComponentEnabled<SquareObstacle>(tower, false);

        manager.SetComponentData(tower, new DestroyComponent { IsNeedToDestroy = true, DestroyDelay = sellTowerTime });

        var cost = manager.GetComponentData<CostComponent>(tower);

        var cashComponent = GetCashComponentRW();
        cashComponent.ValueRW.SellTower(cost);

        var positionComponent = manager.GetComponentData<PositionComponent>(tower);
        CashComponent.SpawnCashUpdatedEvent(manager, cost.SellCost, positionComponent.Position);

        var towerVisual = manager.GetComponentData<TowerVisualComponent>(tower);
        towerVisual.ReleaseVisual();

        // var squareObstacle = manager.GetComponentData<SquareObstacle>(tower);

        var dropZoneEntity = manager.GetComponentData<EntityHolderComponent>(tower).Entity;
        //TODO: check for possible 
        var dropZoneComponent = SetDropZoneOccupied(ref manager, dropZoneEntity, Entity.Null);

        RefRW<BaseFlowField> baseFlowField = GetBaseFlowField();
        GridPositionComponent gridPositionComponent = manager.GetComponentData<GridPositionComponent>(tower);
        baseFlowField.ValueRW.ChangeFlowField(gridPositionComponent.Value, false);
        CreateBaseFieldUpdateEvent();

        Messenger<Entity>.Broadcast(GameEvents.TowerSell, tower, MessengerMode.DONT_REQUIRE_LISTENER);
        Messenger<Entity>.Broadcast(UIEvents.ObjectSelected, Entity.Null, MessengerMode.DONT_REQUIRE_LISTENER);
        Messenger<DropZoneComponent, GridPositionComponent, bool>.Broadcast(GameEvents.DropZoneStateChanged, dropZoneComponent, gridPositionComponent, false, MessengerMode.DONT_REQUIRE_LISTENER);

        return true;
    }

    public void PowerCellClicked(Entity powerCell)
    {
        TimerComponent timerComponent = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<TimerComponent>(powerCell);
        timerComponent.Timer = 0;
        World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(powerCell, timerComponent);

        ReplayManager.Instance.LogPowerCellClicked(powerCell);
    }

    public static DropZoneComponent SetDropZoneOccupied(ref EntityManager manager, Entity dropZone, Entity towerEntity)
    {
        DropZoneComponent dropZoneComponent = manager.GetComponentData<DropZoneComponent>(dropZone);
        if (towerEntity == Entity.Null)
            dropZoneComponent.TimeToReactivate = DropZoneReactivateDropZone;
        else
            dropZoneComponent.IsOccupied = true;

        manager.SetComponentData(dropZone, dropZoneComponent);
        manager.SetComponentData(dropZone, new EntityHolderComponent { Entity = towerEntity });

        Entity dropZoneEvent = manager.CreateEntity();
        manager.SetName(dropZoneEvent, nameof(DropZoneEvent));
        manager.AddComponentData(dropZoneEvent, new DropZoneEvent() { Entity = dropZone });

        return dropZoneComponent;
    }

    // private static SquareObstacle CreateObstacleForTower(ref EntityManager manager, GridPositionComponent dropZoneGrid, Entity towerEntity)
    // {
    //     float2 startPosition = FlowFieldSystemBase.GetCenterPosition(dropZoneGrid.Value.GridPos, new int2(1, 1)).xy + new float2(-0.5f, -0.5f);
    //
    //     SquareObstacle towerSquareObstacle = new();
    //     float2 leftPoint = startPosition + dropZoneGrid.Value.GridSize;
    //     towerSquareObstacle.ObstacleType = AllEnums.ObstacleType.OnlyRicochet;
    //     towerSquareObstacle.StartPoint = startPosition;
    //     towerSquareObstacle.TopPoint = new float2(startPosition.x, leftPoint.y);
    //     towerSquareObstacle.LeftPoint = leftPoint;
    //     towerSquareObstacle.EndPoint = new float2(leftPoint.x, startPosition.y);
    //     manager.AddComponentData(towerEntity, towerSquareObstacle);
    //     return towerSquareObstacle;
    // }

    #endregion

    private void CreateCashEvent(EntityManager manager, int cash, Entity entity)
    {
        var positionComponent = manager.GetComponentData<PositionComponent>(entity);
        CashComponent.SpawnCashUpdatedEvent(manager, cash, positionComponent.Position);
    }

    public void CreateBaseFieldUpdateEvent()
    {
        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        Entity updateFieldEvent = manager.CreateEntity();
        manager.SetName(updateFieldEvent, "UpdateBaseFieldEvent");
        manager.AddComponentData(updateFieldEvent, new BaseCostChangedEvent());
    }

    // public void CreateInFieldUpdateEvent()
    // {
    //     var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
    //     Entity updateInFieldEvent = manager.CreateEntity();
    //     manager.SetName(updateInFieldEvent,"UpdateInFieldEvent");
    //     manager.AddComponentData(updateInFieldEvent, new UpdateInFieldEvent());
    // }

#if UNITY_EDITOR

    [UnityEditor.InitializeOnLoad]
    public static class SingletosEditorHook
    {
        static SingletosEditorHook()
        {
            UnityEditor.EditorApplication.playModeStateChanged += Instance.OnPlayStateChanged;
        }
    }

    internal void OnPlayStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
        {
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
                manager.CompleteAllTrackedJobs();
                WorldSaver.DisposeAndDelete(manager);
            }
        }
    }

    [Button, BoxGroup("Debug methods")]
    private void ShowSharedDatasBackUp()
    {
        if (missionBackUpWorld.IsCreated)
            ShowSharedDatas(missionBackUpWorld);
        else
            Debug.Log("NoBackup world");
    }

    [Button, BoxGroup("Debug methods")]
    private void ShowSharedDatasActives()
    {
        ShowSharedDatas(World.DefaultGameObjectInjectionWorld);
    }

    private void ShowSharedDatas(World world)
    {
        world.EntityManager.GetAllUniqueSharedComponents(out NativeList<SharedRenderData> sharedRenderDatas, Allocator.Temp);
        if (sharedRenderDatas.Length == 1)
            Debug.Log("SharedRenderDatas is empty");
        else
            for (int i = 1; i < sharedRenderDatas.Length; i++)
                Debug.Log("-->" + sharedRenderDatas[i].CreepType);
    }

    [Button, BoxGroup("Debug methods")]
    private void ShowBatchDatas()
    {
        BRGSystem bRGSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<BRGSystem>();
        if (bRGSystem == null)
            Debug.Log("Brg is null");
        else
            bRGSystem.ShowBatchData();
    }
#endif
}