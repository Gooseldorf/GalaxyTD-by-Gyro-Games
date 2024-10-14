using System.Collections.Generic;
using CardTD.UIAndVisual.Visualization;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

[Serializable]
public struct LevelGridMatrix
{
    public Vector2Int Bounds;

    [OdinSerialize, NonSerialized] public byte[,] Background;
    public Vector3Int BackgroundOrigin;

    [OdinSerialize, NonSerialized] public byte[,] BackgroundEffects;
    public Vector3Int BackgroundEffectsOrigin;

    [OdinSerialize, NonSerialized] public byte[,] Floor;
    public Vector3Int FloorOrigin;

    [OdinSerialize, NonSerialized] public byte[,] Walls;
    public Vector3Int WallsOrigin;
}

public class MapCreator : SerializedScriptableObject
{
    private const string missionsFolderPath = "Assets/LevelsScriptableObjects/Missions/";

    public const string CritterSpawn = "CritterSpawn";
    
    [SerializeField, Required] private SimpleEffectManager simpleEffectManager;

    [SerializeField, FoldoutGroup("Tiles")]
    private LevelGrid gridPrefab;

    [SerializeField, FoldoutGroup("Tiles")]
    private List<TileBase> backgroundTiles;

    [SerializeField, FoldoutGroup("Tiles")]
    private List<TileBase> backgroundEffectsTiles;

    [SerializeField, FoldoutGroup("Tiles")]
    private List<TileBase> floorTiles;

    [SerializeField, FoldoutGroup("Tiles")]
    private List<TileBase> wallTiles;

    private LevelGrid grid;
    private int2 gridPositionOffset;

    private LevelGrid levelGrid
    {
        get
        {
            if (grid == null) CreateGridIfNeeded();

            return grid;
        }
    }

    [SerializeField, HideInInspector] private Mission currentMission;

    private void CreateGridIfNeeded()
    {
        if (grid == null)
        {
            LevelGrid levelGrid = FindFirstObjectByType<LevelGrid>();
            grid = levelGrid != null ? levelGrid : Instantiate(gridPrefab);
        }
    }


    [Button, PropertySpace(10)]
    public void DrawMapFromMission(Mission mission)
    {
        if (mission == null) return;
        currentMission = mission;

        CreateLevelGrid(currentMission.LevelMatrix);

        simpleEffectManager.CreateRoots();
        DrawObjects(currentMission);
    }

    private void DrawObjects(Mission currentMission)
    {
        foreach (DropZone dropZone in currentMission.DropZones)
            InitVisual<DropZoneVisual>(dropZone.Clone(), levelGrid.DropZones.transform);

        foreach (ExitPoint exitPoint in currentMission.ExitPoints)
            InitVisual<ExitPointVisual>(exitPoint.Clone(), levelGrid.ExitPoints.transform);

        foreach (Portal portalData in currentMission.Portals)
        {
            Portal portal = new() { In = portalData.In, Out = portalData.Out, IsPowered = portalData.IsPowered, Id = portalData.Id };
            InitVisual<PortalVisual>(portal, levelGrid.Portals.transform);
        }

        foreach (var data in currentMission.SpawnData)
            InitSpawnGroup(data);

        foreach (Bridge bridgeData in currentMission.Bridges)
        {
            Bridge bridge = new() { GridPos = bridgeData.GridPos, GridSize = bridgeData.GridSize, IsPowered = bridgeData.IsPowered, Id = bridgeData.Id };
            InitVisual<BridgeVisual>(bridge, levelGrid.Bridges.transform);
        }

        foreach (Gate gateData in currentMission.Gates)
        {
            Gate gate = new() { GridPos = gateData.GridPos, GridSize = gateData.GridSize, Id = gateData.Id, IsPowered = gateData.IsPowered };
            InitVisual<GateVisual>(gate, levelGrid.Gates.transform);
        }

        if (currentMission.CritterSpawnPoints != null)
        {
            foreach (CritterSpawnPoint critterSpawnPoint in currentMission.CritterSpawnPoints)
            {
                CritterSpawnPoint critterPoint = new() { GridPos = critterSpawnPoint.GridPos, GridSize = critterSpawnPoint.GridSize, CritterStats = critterSpawnPoint.CritterStats, };
                InitVisual<CritterSpawnPointVisual>(critterPoint, levelGrid.CritterSpawnPoints.transform);
            }
        }

        if (currentMission.Conveyors != null)
        {
            foreach (Conveyor conveyor in currentMission.Conveyors)
            {
                InitVisual<ConveyorBeltVisual>(conveyor, levelGrid.Conveyors.transform);
            }
        }

        foreach (EnergyCore energy in currentMission.EnergyCores)
        {
            EnergyCore core = energy.Clone() as EnergyCore;
            InitVisual<EnergyCoreVisual>(core, levelGrid.EnergyCores.transform);
        }

        if (currentMission.PowerSwitchDatas != null)
        {
            foreach (var data in currentMission.PowerSwitchDatas)
            {
                GameObject powerSwitch = new GameObject("PowerSwitch_ " + data.Id);
                powerSwitch.transform.SetParent(levelGrid.PowerSwitches.transform);
                powerSwitch.AddComponent<PowerSwitchCreator>().Init(data);
            }
        }
    }

    private void InitVisual<T>(object target, Transform parent) where T : EnvironmentVisual
    {
        EnvironmentVisual visual = simpleEffectManager.GetSimpleVisual<T>();
        if (visual != null)
        {
            visual.transform.SetParent(parent);
            visual.InitVisual(target);
            if (target is IPowerable powerable && visual is IPowerableVisual powerableVisual)
            {
                powerableVisual.Id = powerable.Id;
                powerableVisual.IsPowered = !powerable.IsPowered;
                powerableVisual.TogglePower();
                powerable.OnTogglePower += powerableVisual.TogglePower;
            }
        }
        else Debug.LogError($"Cant get {nameof(T)}");
    }

    private void InitSpawnGroup(SpawnGroup spawnGroup)
    {
        SpawnGroupVisual spawnZoneVisual = simpleEffectManager.GetSpawnGroupVisual();

        if (spawnZoneVisual != null)
        {
            spawnZoneVisual.transform.SetParent(levelGrid.SpawnZones.transform);
            spawnZoneVisual.Init(spawnGroup, simpleEffectManager);
        }
        else Debug.LogError("Cant get SpawnGroupVisual");
    }

    public void CreateMapFromMission(Mission mission)
    {
        if (mission == null) return;
        currentMission = mission;
        CreateLevelGrid(mission.LevelMatrix);
    }

    private void CreateLevelGrid(LevelGridMatrix gridMatrix)
    {
        levelGrid.Clear();
        FillTilemap(gridMatrix.Background, gridMatrix.BackgroundOrigin, levelGrid.Background, backgroundTiles);
        FillTilemap(gridMatrix.BackgroundEffects, gridMatrix.BackgroundEffectsOrigin, levelGrid.BackgroundEffects, backgroundEffectsTiles);
        FillTilemap(gridMatrix.Floor, gridMatrix.FloorOrigin, levelGrid.Floor, floorTiles);
        FillTilemap(gridMatrix.Walls, gridMatrix.WallsOrigin, levelGrid.Walls, wallTiles);
    }

    private void FillTilemap(byte[,] tileMatrix, Vector3Int origin, Tilemap tilemap, List<TileBase> tileSet)
    {
        for (int x = 0; x < tileMatrix.GetLength(0); x++)
        {
            for (int y = 0; y < tileMatrix.GetLength(1); y++)
            {
                if (tileMatrix[x, y] != byte.MaxValue)
                    tilemap.SetTile(origin + new Vector3Int(x, y, 0), tileSet[tileMatrix[x, y]]);
            }
        }
    }

#if UNITY_EDITOR

    [SerializeField] private Mission linkMission;


    [Button, PropertySpace(10)]
    private void UpdateMission(Mission mission)
    {
        if (mission == null) return;
        currentMission = mission;
        currentMission.LevelMatrix = CreateLevelGridMatrix();
        currentMission.CreateCells();
        SetEnvironments();
        SetObstacles();

        string path = AssetDatabase.GetAssetPath(currentMission);
        AssetDatabase.ForceReserializeAssets(new[] { AssetDatabase.GetAssetPath(currentMission) });
    }

    [Button, PropertySpace(10)]
    private void CreateNewMissionScriptableObject(string missionName = "Mission")
    {
        Mission mission = CreateInstance<Mission>();
        mission.LevelMatrix = CreateLevelGridMatrix();

        string path = $"{missionsFolderPath}{missionName}";
        int assetCount = 0;
        string assetName = $"{path}.asset";

        while (AssetDatabase.AssetPathExists(assetName))
        {
            assetCount++;
            assetName = $"{path}({assetCount}).asset";
        }

        AssetDatabase.CreateAsset(mission, assetName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = mission;

        currentMission = mission;

        SetEnvironments();
        SetObstacles();
    }

    [Button, PropertySpace(10)]
    private void ClearMap()
    {
        levelGrid.Clear();
    }

    private LevelGridMatrix CreateLevelGridMatrix()
    {
        LevelGridMatrix matrix = new();
        levelGrid.Compress();
        Vector3Int originDelta = levelGrid.GetOriginOffset();

        matrix.Bounds = levelGrid.GetGridSize();

        matrix.Background = CreateTileMatrix(levelGrid.Background, backgroundTiles);
        matrix.BackgroundOrigin = levelGrid.Background.origin + originDelta;

        matrix.BackgroundEffects = CreateTileMatrix(levelGrid.BackgroundEffects, backgroundEffectsTiles);
        matrix.BackgroundEffectsOrigin = levelGrid.BackgroundEffects.origin + originDelta;

        matrix.Floor = CreateTileMatrix(levelGrid.Floor, floorTiles);
        matrix.FloorOrigin = levelGrid.Floor.origin + originDelta;

        matrix.Walls = CreateTileMatrix(levelGrid.Walls, wallTiles);
        matrix.WallsOrigin = levelGrid.Walls.origin + originDelta;

        return matrix;
    }

    private byte[,] CreateTileMatrix(Tilemap tilemap, IList<TileBase> tileSet)
    {
        byte[,] tileMatrix = new byte[tilemap.size.x, tilemap.size.y];

        for (int y = 0; y < tilemap.size.y; y++)
        {
            for (int x = 0; x < tilemap.size.x; x++)
            {
                if (!tilemap.HasTile(tilemap.origin + new Vector3Int(x, y)))
                {
                    tileMatrix[x, y] = byte.MaxValue;
                }
                else
                {
                    TileBase tile = tilemap.GetTile(tilemap.origin + new Vector3Int(x, y));
                    tileMatrix[x, y] = (byte)tileSet.IndexOf(tile);
                }
            }
        }

        return tileMatrix;
    }

    private void SetEnvironments()
    {
        lastId = 1;

        Vector3Int offset = levelGrid.GetOriginOffset();
        gridPositionOffset = new int2(offset.x, offset.y);

        ClearIds();

        currentMission.SpawnData = FindSpawnZones();
        currentMission.DropZones = FindDropZones();
        currentMission.ExitPoints = FindExitPoints();

        currentMission.Bridges = FindBridges();

        currentMission.Gates = FindGates();

        currentMission.Portals = FindPortals();
        currentMission.CritterSpawnPoints = FindCritterSpawnPoints();
        currentMission.Conveyors = FindConveyors();

        //Energy Cores should be set at the very end (we have to set Ids to all other objects)
        currentMission.EnergyCores = FindEnergyCores();

        // not exactly an environment object, but have to set it somewhere
        currentMission.PowerSwitchDatas = FindPowerSwitches();
    }

    private PowerSwitchCreator.PowerSwitchData[] FindPowerSwitches()
    {

        PowerSwitchCreator[] powerSwitchCreators = FindObjectsByType<PowerSwitchCreator>(FindObjectsSortMode.None);
        PowerSwitchCreator.PowerSwitchData[] powerSwitchDatas = new PowerSwitchCreator.PowerSwitchData[powerSwitchCreators.Length];
        for (int i = 0; i < powerSwitchCreators.Length; i++)
        {
            powerSwitchDatas[i] = powerSwitchCreators[i].GetData();

            powerSwitchCreators[i].Id = GetNewId(powerSwitchDatas[i]);
            powerSwitchDatas[i].Id = powerSwitchCreators[i].Id;
        }
        return powerSwitchDatas;
    }

    private SpawnGroup[] FindSpawnZones()
    {
        SpawnGroupVisual[] spawnGroupVisuals = FindObjectsByType<SpawnGroupVisual>(FindObjectsSortMode.None);
        SpawnGroup[] spawnGroups = new SpawnGroup[spawnGroupVisuals.Length];

        for (int i = 0; i < spawnGroupVisuals.Length; i++)
        {
            spawnGroupVisuals[i].Zones = spawnGroupVisuals[i].GetComponentsInChildren<SpawnZonePartialVisual>();
            spawnGroups[i] = spawnGroupVisuals[i].GetSpawnGroup(gridPositionOffset);
        }

        return spawnGroups;
    }

    private DropZone[] FindDropZones()
    {
        DropZoneVisual[] dropZoneVisuals = FindObjectsByType<DropZoneVisual>(FindObjectsSortMode.None);
        DropZone[] dropZones = new DropZone[dropZoneVisuals.Length];

        //List<int> FindIds = new();

        for (int i = 0; i < dropZones.Length; i++)
        {
            dropZones[i] = dropZoneVisuals[i].GetDropZoneData(gridPositionOffset);
            //if (dropZoneVisuals[i].Id == 0)
            dropZoneVisuals[i].Id = GetNewId(dropZones[i]);

            dropZones[i].Id = dropZoneVisuals[i].Id;

            /*if (FindIds.Contains(dropZones[i].Id))
            {
                Debug.LogError($"id {dropZones[i].Id} is busy");
            }*/

            //FindIds.Add(dropZones[i].Id);
        }

        return dropZones;
    }

    private ExitPoint[] FindExitPoints()
    {
        ExitPointVisual[] exitZonesVisuals = FindObjectsByType<ExitPointVisual>(FindObjectsSortMode.None);
        ExitPoint[] exitPoints = new ExitPoint[exitZonesVisuals.Length];

        for (int i = 0; i < exitPoints.Length; i++)
        {
            exitPoints[i] = exitZonesVisuals[i].GetExitPointData(gridPositionOffset);
        }

        return exitPoints;
    }

    private Portal[] FindPortals()
    {
        PortalVisual[] portalVisuals = FindObjectsByType<PortalVisual>(FindObjectsSortMode.None);

        Portal[] portals = new Portal[portalVisuals.Length];

        for (int i = 0; i < portals.Length; i++)
        {
            portals[i] = portalVisuals[i].GetPortalData(gridPositionOffset);
            portals[i].Id = GetNewId(portals[i]);
            portalVisuals[i].Id = portals[i].Id;
        }

        return portals;
    }

    private Bridge[] FindBridges()
    {
        BridgeVisual[] bridgeVisuals = FindObjectsByType<BridgeVisual>(FindObjectsSortMode.None);

        Bridge[] bridges = new Bridge[bridgeVisuals.Length];

        for (int i = 0; i < bridges.Length; i++)
        {
            bridges[i] = bridgeVisuals[i].GetBridgeData(levelGrid.Objects, gridPositionOffset);
            bridges[i].Id = GetNewId(bridges[i]);
            bridgeVisuals[i].Id = bridges[i].Id;
        }

        return bridges;
    }

    private Gate[] FindGates()
    {
        GateVisual[] gateVisuals = FindObjectsByType<GateVisual>(FindObjectsSortMode.None);

        Gate[] gates = new Gate[gateVisuals.Length];

        for (int i = 0; i < gates.Length; i++)
        {
            gates[i] = gateVisuals[i].GetGateData(gridPositionOffset, grid.Objects);
            gates[i].Id = GetNewId(gates[i]);
            gateVisuals[i].Id = gates[i].Id;
        }

        return gates;
    }

    private CritterSpawnPoint[] FindCritterSpawnPoints()
    {
        CritterSpawnPointVisual[] critterVisuals = FindObjectsByType<CritterSpawnPointVisual>(FindObjectsSortMode.None);

        CritterSpawnPoint[] critterSpawnPoints = new CritterSpawnPoint[critterVisuals.Length];

        for (int i = 0; i < critterSpawnPoints.Length; i++)
        {
            critterSpawnPoints[i] = critterVisuals[i].GetData();
        }

        return critterSpawnPoints;
    }

    private Conveyor[] FindConveyors()
    {
        ConveyorBeltVisual[] conveyorVisuals = FindObjectsByType<ConveyorBeltVisual>(FindObjectsSortMode.None);
        Conveyor[] conveyors = new Conveyor[conveyorVisuals.Length];
        for (int i = 0; i < conveyors.Length; i++)
        {
            conveyors[i] = conveyorVisuals[i].GetConveyorBeltData(gridPositionOffset);
            conveyors[i].Id = GetNewId(conveyors[i]);
            conveyorVisuals[i].Id = conveyors[i].Id;
        }
        return conveyors;
    }

    private EnergyCore[] FindEnergyCores()
    {
        EnergyCoreVisual[] coreVisuals = FindObjectsByType<EnergyCoreVisual>(FindObjectsSortMode.None);

        EnergyCore[] cores = new EnergyCore[coreVisuals.Length];

        for (int i = 0; i < cores.Length; i++)
        {
            cores[i] = coreVisuals[i].GetEnergyCoreData(gridPositionOffset);
            cores[i].Id = lastId++;
            coreVisuals[i].Id = cores[i].Id;
        }

        return cores;
    }

    [Button]
    private void ShowObstacles()
    {
        levelGrid.CurrentMission = currentMission;
        levelGrid.ShowObstacles = true;
    }

    [Button]
    private void HideObstacles()
    {
        levelGrid.CurrentMission = null;
        levelGrid.ShowObstacles = false;
    }

    private void SetObstacles()
    {
        int width = currentMission.LevelMatrix.Walls.GetLength(0);
        int height = currentMission.LevelMatrix.Walls.GetLength(1);
        List<IObstacle> result = new List<IObstacle>();

        for (int i = 0; i < width; i++)
        {
            result.AddRange(GenerateObstacles(height, true, i));
        }

        for (int j = 0; j < height; j++)
        {
            result.AddRange(GenerateObstacles(width, false, j));
        }

        GateVisual[] gateVisuals = FindObjectsByType<GateVisual>(FindObjectsSortMode.None);
        foreach (var gateVisual in gateVisuals)
        {
            result.Add(CreateObstacle(levelGrid.Objects.WorldToCell(gateVisual.StartPart.position)));
            result.Add(CreateObstacle(levelGrid.Objects.WorldToCell(gateVisual.EndPart.position)));
        }

        currentMission.Obstacles = result.ToArray();
    }

    private List<IObstacle> GenerateObstacles(int length, bool isRow, int index)
    {
        List<IObstacle> obstacles = new List<IObstacle>();
        int? wallStart = null;

        for (int cell = 0; cell <= length; cell++)
        {
            bool isWallCell;

            if (isRow)
                isWallCell = cell < length && currentMission.LevelMatrix.Walls[index, cell] != byte.MaxValue;
            else
                isWallCell = cell < length && currentMission.LevelMatrix.Walls[cell, index] != byte.MaxValue;

            if (isWallCell)
            {
                if (wallStart == null)
                {
                    wallStart = cell;
                }
            }
            else if (wallStart != null)
            {
                int wallEnd = cell - 1;
                if (wallEnd > wallStart)
                {
                    int2 start = isRow ? new int2(index, (int)wallStart) : new int2((int)wallStart, index);
                    int2 end = isRow ? new int2(index, wallEnd) : new int2(wallEnd, index);

                    obstacles.Add(CreateObstacle(start, end,
                        new int2(currentMission.LevelMatrix.WallsOrigin.x, currentMission.LevelMatrix.WallsOrigin.y)));
                }

                wallStart = null;
            }
        }

        return obstacles;
    }

    private IObstacle CreateObstacle(int2 start, int2 end, int2 offset)
    {
        float2[] points = new float2[4];

        points[0] = new float2(start.x, start.y) + offset;
        points[1] = new float2(start.x, end.y + 1) + offset;

        points[2] = new float2(end.x + 1, end.y + 1) + offset;
        points[3] = new float2(end.x + 1, start.y) + offset;

        return new SquareMapObstacle(points, AllEnums.ObstacleType.OnlyRicochet);
    }

    private IObstacle CreateObstacle(Vector3Int cellPosition)
    {
        float2[] points = new float2[4];

        points[0] = new float2(cellPosition.x, cellPosition.y);
        points[1] = new float2(cellPosition.x, cellPosition.y + 1);

        points[2] = new float2(cellPosition.x + 1, cellPosition.y + 1);
        points[3] = new float2(cellPosition.x + 1, cellPosition.y);

        return new SquareMapObstacle(points, AllEnums.ObstacleType.OnlyRicochet);
    }

    private const int portalStartId = 100000;
    private const int gateStartId = 110000;
    private const int bridgeStartId = 120000;
    private const int dropZoneStartId = 130000;
    private const int conveyorStartId = 140000;
    private const int powerSwitchStartId = 150000;

    private int lastId;
    private int lastPortalId;
    private int lastGateId;
    private int lastBridgeId;
    private int lastDropZoneId;
    private int lastConveyorId;
    private int lastPowerSwitchId;

   
    public int GetNewId(object identifiable)
    {
        return identifiable switch
        {
            Portal => portalStartId + lastPortalId++,
            Gate => gateStartId + lastGateId++,
            Bridge => bridgeStartId + lastBridgeId++,
            DropZone => dropZoneStartId + lastDropZoneId++,
            Conveyor => conveyorStartId + lastConveyorId++,
            PowerSwitchCreator.PowerSwitchData => powerSwitchStartId + lastPowerSwitchId++,
            _ => throw new Exception(
                $"[{nameof(MapCreator)}] {nameof(GetNewId)}, Add new rule for setting Id: {identifiable.GetType()}")
        };
    }

    private void ClearIds()
    {
        lastId = 0;
        lastPortalId = 0;
        lastGateId = 0;
        lastBridgeId = 0;
        lastDropZoneId = 0;
        lastConveyorId = 0;
    }

    [SerializeField, FoldoutGroup(CritterSpawn)]
    private CritterStats critterStats;

    [SerializeField, FoldoutGroup(CritterSpawn)]
    private List<CritterSpawnPoint> critterSpawnPoints = new();

    [Button, FoldoutGroup(CritterSpawn)]
    private void ReadCritters()
    {
        if (linkMission == null)
        {
            Debug.LogError("Needed add link mission");
            return;
        }

        critterSpawnPoints = new List<CritterSpawnPoint>(linkMission.CritterSpawnPoints);
    }

    [Button, FoldoutGroup(CritterSpawn)]
    private void AddAllCritters()
    {
        AddCrittersToEnergyCores();
        AddCrittersToPortals();
        AddCrittersToExitPoints();
        AddCrittersToSpawnPoints();
    }

    [Button, FoldoutGroup(CritterSpawn)]
    private void UpdateCritters()
    {
        if (linkMission == null)
            return;
        linkMission.CritterSpawnPoints = critterSpawnPoints.ToArray();
        AssetDatabase.ForceReserializeAssets(new[] { AssetDatabase.GetAssetPath(linkMission) });
    }

    [Button, FoldoutGroup(CritterSpawn)]
    private void AddCrittersToEnergyCores()
    {
        if (linkMission == null)
            return;
        foreach (EnergyCore core in linkMission.EnergyCores)
            AddCritter(core.GridPos);
    }

    [Button, FoldoutGroup(CritterSpawn)]
    private void AddCrittersToPortals()
    {
        if (linkMission == null)
            return;
        foreach (Portal portal in linkMission.Portals)
        {
            AddCritter(portal.In.GridPos);
            AddCritter(portal.Out.GridPos);
        }
    }

    [Button, FoldoutGroup(CritterSpawn)]
    private void AddCrittersToExitPoints()
    {
        if (linkMission == null)
            return;
        foreach (ExitPoint exitPoint in linkMission.ExitPoints)
            AddCritter(exitPoint.GridPos);
    }

    [Button, FoldoutGroup(CritterSpawn)]
    private void AddCrittersToSpawnPoints()
    {
        if (linkMission == null)
            return;
        foreach (SpawnGroup spawn in linkMission.SpawnData)
            foreach (GridPosition position in spawn.SpawnPositions)
                AddCritter(position.GridPos);
    }

    private void AddCritter(int2 position)
    {
        critterSpawnPoints.Add(new CritterSpawnPoint() { CritterStats = critterStats, GridPos = position + new int2(1, 1), });
    }

    [Button]
    private void ChangeGroupSelectionBehaviour(bool selectOnlyParent)
    {
        if (selectOnlyParent)
            Selection.selectionChanged += OnSelectionChanged;
        else
            Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        if (Selection.transforms.Length > 0)
        {
            List<Object> selectedParents = new List<Object>();
            foreach (Transform t in Selection.transforms)
            {
                if (t.parent != null && t.parent.GetComponent<DropZoneVisual>() != null)
                {
                    selectedParents.Add(t.parent.gameObject);
                }
            }

            if (selectedParents.Count > 0)
            {
                EditorApplication.delayCall += () =>
                {
                    Selection.objects = selectedParents.ToArray();
                };
            }
        }
    }

#endif
}