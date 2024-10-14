using ECSTest.Components;
using ECSTest.Structs;
using ECSTest.Systems;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UIElements;
using static AllEnums;
using static PowerSwitchCreator;

[CreateAssetMenu(fileName = "Mission", menuName = "ScriptableObjects/Mission")]
public class Mission : SerializedScriptableObject
{
    public int MissionIndex;
    public float HpModifier = 1;
    public float MassModifier = 1;
    public bool IgnoreByMissionSpawnGeneratorParser;

    public List<int> CashPerWaveStart = new() { 100 };

    [OdinSerialize, NonSerialized, ShowInInspector]
    public SpawnGroup[] SpawnData;

    public List<CreepStats> CreepStatsPerWave = new();

    public bool DropZoneCanInfluenceToFlowField;

    [Tooltip("Не проверям если от ядра всегда есть путь к выходу, чинит то что к выходам ведут только порталы")]
    [ShowIf("DropZoneCanInfluenceToFlowField")]
    public bool CheckExits = true;
    public DropZone[] DropZones;



    public float GetRoguelikeHpModifer(int index, int iteration) => HpModifier + (HpModifier * GetHpModiferByUnitType(CreepStatsPerWave[index].CreepType) * CreepStatsPerWave.Count * iteration);

    private float GetHpModiferByUnitType(CreepType type) =>
        type switch
        {
            CreepType.Bio1 => .5f,
            CreepType.Bio2 => .75f,
            CreepType.Bio3 => 1f,
            CreepType.Energy1 => .5f,
            CreepType.Energy2 => .75f,
            CreepType.Energy3 => 1f,
            CreepType.Mech1 => .5f,
            CreepType.Mech2 => .75f,
            CreepType.Mech3 => 1f,
            _ => 0.5f
        };


    [Button]
    private void CheckDropZoneForDuplicate()
    {
        List<DropZone> dropZones = new(DropZones);
        for (int i = 0; i < dropZones.Count - 1; i++)
        {
            for (int j = i + 1; j < dropZones.Count; j++)
            {
                if (dropZones[j].GridPos.Equals(dropZones[i].GridPos))
                {
                    dropZones.RemoveAt(i);
                    i--;
                    break;
                }
            }
        }

        if (dropZones.Count != DropZones.Length)
        {
            Debug.Log($"has duplicates count: {DropZones.Length - dropZones.Count}");
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            DropZones = dropZones.ToArray();
            AssetDatabase.SaveAssets();
#endif
        }
        else
        {
            Debug.Log($"no duplicate");
        }
    }

    [OdinSerialize, NonSerialized, HideInInspector]
    public LevelGridMatrix LevelMatrix;

    public EnergyCore[] EnergyCores;
    [SerializeReference] public IObstacle[] Obstacles;
    public ExitPoint[] ExitPoints;
    public Portal[] Portals;
    public Bridge[] Bridges;
    public Gate[] Gates;
    public CritterSpawnPoint[] CritterSpawnPoints;
    public int MinCritterCountOnStart = 10;
    public Conveyor[] Conveyors;
    public Reward Reward;
    public PowerSwitchData[] PowerSwitchDatas;

    private static int powerCellIds = 0;
    private const int startPowerCellIndex = 3500;

    public int WavesCount
    {
        get
        {
            int maxWavesCount = 0;
            foreach (SpawnGroup spawnGroup in SpawnData)
            {
                foreach (Wave wave in spawnGroup.Waves)
                {
                    maxWavesCount = Math.Max(wave.WaveNum + 1, maxWavesCount);
                }
            }

            return maxWavesCount;
        }
    }

    public Cell[,] GetCellForDropZones()
    {
        Cell[,] cells = CreateCells();

        foreach (Bridge bridge in Bridges)
        {
            ChangeCells(bridge.GridPos, bridge.GridSize, false);
        }

        foreach (Gate gate in Gates)
        {
            var grid = gate.GetCentralPart();
            ChangeCells(grid.GridPos, grid.GridSize, false);
        }

        return cells;

        void ChangeCells(int2 position, int2 size, bool isLockState)
        {
            float2 startPosition = BaseFlowField.GetCenterPosition(position, new int2(1, 1)).xy + new float2(-0.5f, -0.5f);
            float2 leftPoint = startPosition + size;

            int2 topLeftPoint = (int2)startPosition;
            int2 botRightPoint = (int2)leftPoint;

            for (int x = topLeftPoint.x; x < botRightPoint.x; x++)
            {
                for (int y = topLeftPoint.y; y < botRightPoint.y; y++)
                {
                    Cell cell = cells[x, y];
                    if (isLockState && !cell.IsWall)
                    {
                        cell.SetLockCost();
                    }
                    else if (cell.IsWall && !isLockState)
                    {
                        cell.SetDefaultCost();
                    }

                    cells[x, y] = cell;
                }
            }
        }
    }

    public Cell[,] CreateCells()
    {
        Cell[,] cells = new Cell[LevelMatrix.Bounds.x, LevelMatrix.Bounds.y];

        for (int x = 0; x < LevelMatrix.Bounds.x; x++)
        {
            for (int y = 0; y < LevelMatrix.Bounds.y; y++)
            {
                cells[x, y] = new Cell();
                cells[x, y].MoveSpeedModifier = 1;
                cells[x, y].SetLockCost();
            }
        }

        for (int x = 0; x < LevelMatrix.Floor.GetLength(0); x++)
        {
            for (int y = 0; y < LevelMatrix.Floor.GetLength(1); y++)
            {
                if (LevelMatrix.Floor[x, y] != byte.MaxValue)
                {
                    cells[LevelMatrix.FloorOrigin.x + x, LevelMatrix.FloorOrigin.y + y].SetDefaultCost();
                }
            }
        }

        for (int x = 0; x < LevelMatrix.Walls.GetLength(0); x++)
        {
            for (int y = 0; y < LevelMatrix.Walls.GetLength(1); y++)
            {
                if (LevelMatrix.Walls[x, y] != byte.MaxValue)
                {
                    cells[LevelMatrix.WallsOrigin.x + x, LevelMatrix.WallsOrigin.y + y].SetLockCost();
                }
            }
        }

        //this is dirty fix for small bridgeFloorTiles => if we change bridge.GridSize should be trouble with nearby cells LockCost
        foreach (var bridge in Bridges)
        {
            if (bridge.GridSize.x > 2) //horizontal
            {
                cells[bridge.GridPos.x, bridge.GridPos.y - 1].SetLockCost(); // bottomLeft
                cells[bridge.GridPos.x + bridge.GridSize.x - 1, bridge.GridPos.y - 1].SetLockCost(); // bottomRight

                cells[bridge.GridPos.x, bridge.GridPos.y + bridge.GridSize.y].SetLockCost(); // topLeft
                cells[bridge.GridPos.x + bridge.GridSize.x - 1, bridge.GridPos.y + bridge.GridSize.y].SetLockCost(); // topRight
            }
            else //vertical
            {
                cells[bridge.GridPos.x - 1, bridge.GridPos.y].SetLockCost(); // bottomLeft
                cells[bridge.GridPos.x + bridge.GridSize.x, bridge.GridPos.y].SetLockCost(); // bottomRight

                cells[bridge.GridPos.x - 1, bridge.GridPos.y + bridge.GridSize.y - 1].SetLockCost(); // topLeft
                cells[bridge.GridPos.x + bridge.GridSize.x, bridge.GridPos.y + bridge.GridSize.y - 1].SetLockCost(); // topRight
            }

            cells[bridge.GridPos.x, bridge.GridPos.y].SetLockCost(); // centerLeft
            cells[bridge.GridPos.x + bridge.GridSize.x - 1, bridge.GridPos.y].SetLockCost(); // centerRight
            cells[bridge.GridPos.x, bridge.GridPos.y + bridge.GridSize.y - 1].SetLockCost(); // centerLeft
            cells[bridge.GridPos.x + bridge.GridSize.x - 1, bridge.GridPos.y + bridge.GridSize.y - 1].SetLockCost(); // centerRight
        }

        return cells;
    }


    internal void InstantiateObjects(out NativeParallelMultiHashMap<Entity, Entity> connectedPowerables)
    {
        powerCellIds = 0;

        EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

        connectedPowerables = new NativeParallelMultiHashMap<Entity, Entity>(100, Allocator.Persistent);

        Dictionary<int, Entity> idsToEntity = new();
        foreach (Portal portalData in Portals)
            CreatePortal(manager, portalData, idsToEntity);
        foreach (var bridge in Bridges)
            CreateBridge(manager, bridge, idsToEntity);
        foreach (var gate in Gates)
            CreateGate(manager, gate, idsToEntity);
        foreach (var dropZone in DropZones)
            CreateDropZone(manager, dropZone, idsToEntity);
        foreach (var conveyor in Conveyors)
            CreateConveyors(manager, conveyor, idsToEntity);

        //And after that we create Cores using Ids Dictionary
        foreach (EnergyCore core in EnergyCores)
            CreateCore(manager, core, ((IPosition)core).Position.xy, idsToEntity, ref connectedPowerables, core.DeactivationTime);

        foreach (var exitPoint in ExitPoints)
            CreateExitPoint(manager, exitPoint);

        foreach (var spawnGroup in SpawnData)
            CreateSpawnGroup(manager, spawnGroup);

        foreach (IObstacle obstacle in Obstacles)
            if (obstacle is ISquareObstacle squareObstacle)
                CreateSquareObstacle(manager, squareObstacle);

        if (PowerSwitchDatas != null)
            foreach (var powerSwitch in PowerSwitchDatas)
                CreatePowerSwitch(manager, powerSwitch, idsToEntity);
    }

    private void CreatePowerSwitch(EntityManager manager, PowerSwitchData data, Dictionary<int, Entity> idsToEntity)
    {
        Entity powerSwitchEntity = manager.CreateEntity();
        manager.SetName(powerSwitchEntity, nameof(PowerSwitch));

        manager.AddComponentData(powerSwitchEntity, new PowerSwitch { IsTurnedOn = data.IsTurnedOn });
        manager.AddComponentData(powerSwitchEntity, new Timer { Activated = false, Value = data.Timer, Activations = data.Activations, TimeBetweenToggles = data.TimeBetweenToggles });
        var buffer = manager.AddBuffer<EntitiesBuffer>(powerSwitchEntity);

        foreach (int powerableId in data.Powerables)
            buffer.Add(idsToEntity[powerableId]);

        idsToEntity.Add(data.Id, powerSwitchEntity);
    }

    private void CreateGate(EntityManager manager, Gate gate, Dictionary<int, Entity> idsToEntity)
    {
        Entity gateEntity = manager.CreateEntity();

        manager.AddComponentData(gateEntity, new PowerableComponent(gate.IsPowered));
        manager.AddComponentData(gateEntity, new GridPositionComponent { Value = gate.GetGrid() });

        List<GridPositionStruct> boundaryPoints = gate.GetBoundaryPoints();

        manager.AddComponentData(gateEntity,
            new GateComponent
            {
                StartPosition = new GridPositionStruct { GridPos = boundaryPoints[0].GridPos, GridSize = boundaryPoints[0].GridSize },
                EndPosition = new GridPositionStruct { GridPos = boundaryPoints[1].GridPos, GridSize = boundaryPoints[1].GridSize },
                MiddlePosition = gate.GetCentralPart(),
            });

        foreach (GridPositionStruct grid in boundaryPoints)
            CreateSquareObstacle(manager, grid);

        idsToEntity.Add(gate.Id, gateEntity);
    }

    private void CreateBridge(EntityManager manager, Bridge bridge, Dictionary<int, Entity> idsToEntity)
    {
        Entity bridgeEntity = manager.CreateEntity();
        manager.AddComponentData(bridgeEntity, new PowerableComponent(bridge.IsPowered));
        manager.AddComponentData(bridgeEntity, new GridPositionComponent(bridge.GridPos, bridge.GridSize));
        manager.AddComponentData(bridgeEntity, new BridgeComponent());
        //If you have duplicate Id here it' your job to fix Mission Asset!!!
        idsToEntity.Add(bridge.Id, bridgeEntity);
    }

    private void CreateConveyors(EntityManager manager, Conveyor conveyorData, Dictionary<int, Entity> idsToEntity)
    {
        Entity conveyorEntity = manager.CreateEntity();
        manager.SetName(conveyorEntity, $"{nameof(Conveyor)}_{conveyorData.Id}");
        manager.AddComponentData(conveyorEntity, new ConveyorComponent(conveyorData));
        manager.AddComponentData(conveyorEntity, new PowerableComponent() { IsPowered = conveyorData.IsPowered, Reversed = false });
        manager.AddComponentData(conveyorEntity, new GridPositionComponent(conveyorData.GridPos, conveyorData.GridSize));
        idsToEntity.Add(conveyorData.Id, conveyorEntity);
    }

    private static void CreatePortal(EntityManager manager, Portal portalData, Dictionary<int, Entity> idsToEntity)
    {
        Entity portalEntity = manager.CreateEntity();
        manager.SetName(portalEntity, $"{nameof(Portal)}_{portalData.Id}");
        manager.AddComponentData(portalEntity, new PortalComponent(portalData));
        manager.AddComponentData(portalEntity, new PowerableComponent(portalData.IsPowered));
        //If you have duplicate Id here it' your job to fix Mission Asset!!!
        idsToEntity.Add(portalData.Id, portalEntity);
    }

    private void CreateDropZone(EntityManager manager, DropZone dropZone, Dictionary<int, Entity> idsToEntity)
    {
        int id = dropZone.Id;
        while (idsToEntity.ContainsKey(id))
        {
            Debug.LogError($"IdsToEntity ContainsKey : {id}");
            id += 10;
        }

        Entity dropZoneEntity = manager.CreateEntity();

        manager.SetName(dropZoneEntity, $"{nameof(DropZone)}_{id}");
        manager.AddComponentData(dropZoneEntity,
            new DropZoneComponent { IsOccupied = false, IsPossibleToBuild = true, IsCanInfluenceToFlowField = dropZone.IsCanInfluenceToFlowField, TimeToReactivate = 0 });

        float2 dir = new(UnityEngine.Random.Range(-1.0f, 1.0f), UnityEngine.Random.Range(-1.0f, 1.0f));
        if (dir.Equals(float2.zero))
            dir = 1;
        manager.AddComponentData(dropZoneEntity, new PositionComponent(((IPosition)dropZone).Position.xy, math.normalize(dir)));
        manager.AddComponentData(dropZoneEntity, new GridPositionComponent(dropZone.GridPos, dropZone.GridSize));
        manager.AddComponentData(dropZoneEntity, new DestroyComponent { IsNeedToDestroy = false });
        manager.AddComponentData(dropZoneEntity, new EntityHolderComponent { Entity = Entity.Null });
        manager.AddComponentData(dropZoneEntity, new PowerableComponent(dropZone.IsPowered));
        manager.AddComponentData(dropZoneEntity, new Identifiable { Id = id });
        idsToEntity.Add(id, dropZoneEntity);
    }

    private static void CreateCore(EntityManager manager, EnergyCore core, float2 position,
        Dictionary<int, Entity> idsToEntity, ref NativeParallelMultiHashMap<Entity, Entity> connectedPowerables, float deactivationTime)
    {
        Entity coreEntity = manager.CreateEntity();
        manager.SetName(coreEntity, nameof(EnergyCore));
        manager.AddComponentData(coreEntity, new EnergyCoreComponent { PowerCellCount = core.PowerCellCount, IsTurnedOn = true, DeactivationTime = deactivationTime });
        manager.AddComponentData(coreEntity, new PositionComponent { Position = position });
        manager.AddComponentData(coreEntity, new GridPositionComponent(core.GridPos, core.GridSize));

        //!!!! If you have an error here it means that The Map had IPowerable that was connected to several Cores or an invalid reference to Ipowerable
        // If you have error here, and instead of fixing it you  would just comment this section. or ignore it 
        // I will look for you, I will find you and I will kill you
        for (int i = 0; i < core.Powerables.Count; i++)
        {
            Entity connectedPowerable = idsToEntity[core.Powerables[i]];
            connectedPowerables.Add(coreEntity, connectedPowerable);
            idsToEntity.Remove(core.Powerables[i]);
        }

        for (int i = 0; i < core.PowerCellCount; i++)
            CreatePowerCell(manager, coreEntity, position);
    }

    public static void CreatePowerCell(EntityManager manager, Entity coreEntity, float2 position)
    {
        Entity powerCellEntity = manager.CreateEntity();
        manager.SetName(powerCellEntity, "PowerCell");
        manager.AddComponentData(powerCellEntity, new PowerCellComponent { Creep = Entity.Null, CurrentCore = coreEntity, IsMoves = false });
        manager.AddComponentData(powerCellEntity, new PositionComponent { Position = position, Direction = new float2(1, 1) });
        manager.AddComponentData(powerCellEntity, new DestroyComponent { IsNeedToDestroy = false });
        manager.AddComponentData(powerCellEntity, new TimerComponent(PowerCellSystemBase.ReturnTime));
        manager.AddComponentData(powerCellEntity, new Identifiable { Id = startPowerCellIndex + powerCellIds });
        powerCellIds++;
    }

    private void CreateExitPoint(EntityManager manager, ExitPoint exitPoint)
    {
        Entity exitPointEntity = manager.CreateEntity();
        manager.SetName(exitPointEntity, nameof(ExitPoint));
        manager.AddComponentData(exitPointEntity, new ExitPointComponent());
        manager.AddComponentData(exitPointEntity, new GridPositionComponent(exitPoint.GridPos, exitPoint.GridSize));
        manager.AddComponentData(exitPointEntity, new PositionComponent { Position = ((IPosition)exitPoint).Position.xy });
    }

    private void CreateSpawnGroup(EntityManager manager, SpawnGroup spawnGroup)
    {
        foreach (var spawnZone in spawnGroup.SpawnPositions)
        {
            Entity spawnZoneEntity = manager.CreateEntity();
            manager.SetName(spawnZoneEntity, "SpawnZone");
            manager.AddComponentData(spawnZoneEntity, new SpawnZoneComponent());
            manager.AddComponentData(spawnZoneEntity, new GridPositionComponent(spawnZone.GridPos, spawnZone.GridSize));
            manager.AddComponentData(spawnZoneEntity, new PositionComponent { Position = ((IPosition)spawnZone).Position.xy });
        }
    }

    private void CreateSquareObstacle(EntityManager manager, ISquareObstacle squareObstacle)
    {
        var entity = manager.CreateEntity();
        manager.SetName(entity, nameof(SquareObstacle));

        manager.AddComponentData(entity,
            new SquareObstacle
            {
                ObstacleType = squareObstacle.ObstacleType,
                BotLeftPoint = squareObstacle.Points[0],
                TopLeftPoint = squareObstacle.Points[1],
                TopRightPoint = squareObstacle.Points[2],
                BotRightPoint = squareObstacle.Points[3],
            });
    }

    private void CreateSquareObstacle(EntityManager manager, GridPositionStruct gridPositionStruct)
    {
        var entity = manager.CreateEntity();
        manager.SetName(entity, nameof(SquareObstacle));

        float2 startPosition = BaseFlowField.GetCenterPosition(gridPositionStruct.GridPos, new int2(1, 1)).xy + new float2(-0.5f, -0.5f);
        float2 leftPoint = startPosition + gridPositionStruct.GridSize;

        manager.AddComponentData(entity,
            new SquareObstacle
            {
                ObstacleType = ObstacleType.OnlyRicochet,
                BotLeftPoint = startPosition,
                TopLeftPoint = new float2(startPosition.x, leftPoint.y),
                TopRightPoint = leftPoint,
                BotRightPoint = new float2(leftPoint.x, startPosition.y),
            });
    }

#if UNITY_EDITOR
    [Button]
    private void ReSaveAsset()
    {
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
#endif

    //[Button]
    //private void DirtyCreateEmptyWaves()
    //{
    //    for (int k = 0; k < SpawnData.Length; k++)
    //    {
    //        SpawnData[k].Waves.Clear();
    //        for (int i = 0; i < 12; i++)
    //        {
    //            Wave tempWave = new Wave() { WaveNum = i };
    //            SpawnData[k].Waves.Add(tempWave);
    //        }
    //    }
    //}
}