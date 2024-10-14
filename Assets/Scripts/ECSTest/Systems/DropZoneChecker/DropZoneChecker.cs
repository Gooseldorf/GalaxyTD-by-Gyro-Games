using CardTD.Utilities;
using ECSTest.Components;
using ECSTest.Structs;
using Sirenix.Utilities;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECSTest.Systems.DropZoneChecker
{
    public class DropZoneChecker : MonoBehaviour
    {
        private EntityQuery dropZoneQuery;
        private Mission mission;
        private readonly List<PairList> areas = new();

        private readonly int2[] positions = {new(-1, -1), new(-1, 0), new(-1, 1), new(-1, 2), new(0, 2), new(1, 2), new(2, 2), new(2, 1), new(2, 0), new(2, -1), new(1, -1), new(0, -1)};

        private readonly List<int2> startPoints = new();
        private readonly List<HashSet<int2>> energyCoresCount = new();
        private readonly List<HashSet<int2>> countSpawnGroupsPerStartPoint = new();
        private readonly List<Portal> portals = new();
        private readonly HashSet<int2> exitPoints = new();
        private readonly List<SpawnGroup> spawnGroups = new();

        private Cell[,] gridMap;

        private void Awake()
        {
            mission = GameServices.Instance.CurrentMission;
            if (!mission.DropZoneCanInfluenceToFlowField) return;

            Messenger.AddListener(GameEvents.Restart, RestartGame);
            Messenger<DropZoneComponent, GridPositionComponent, bool>.AddListener(GameEvents.DropZoneStateChanged, DropZoneStateChanged);

            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            dropZoneQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<DropZoneComponent, GridPositionComponent, Identifiable>().Build(manager);

            foreach (EnergyCore energyCore in mission.EnergyCores)
            {
                startPoints.Add(energyCore.GridPos);
                countSpawnGroupsPerStartPoint.Add(new());
                energyCoresCount.Add(new());
            }

            foreach (Portal portal in mission.Portals)
            {
                portals.Add(portal);
            }

            foreach (ExitPoint exitPoint in mission.ExitPoints)
            {
                int2 startExitPoint = exitPoint.GridPos;

                for (int i = 0; i < exitPoint.GridSize.x; i++)
                for (int j = 0; j < exitPoint.GridSize.y; j++)
                    exitPoints.Add(new int2(startExitPoint.x + i, startExitPoint.y + j));
            }


            foreach (SpawnGroup spawnGroup in mission.SpawnData)
                spawnGroups.Add(spawnGroup);
        }

        private void OnDestroy()
        {
            if (!mission.DropZoneCanInfluenceToFlowField) return;
            Messenger.RemoveListener(GameEvents.Restart, RestartGame);
            Messenger<DropZoneComponent, GridPositionComponent, bool>.RemoveListener(GameEvents.DropZoneStateChanged, DropZoneStateChanged);
        }

        public void Start()
        {
            if (!mission.DropZoneCanInfluenceToFlowField)
                return;
            gridMap = mission.GetCellForDropZones();
            CalculateTree();
        }

        private void RestartGame()
        {
            if (!mission.DropZoneCanInfluenceToFlowField)
                return;
            gridMap = mission.GetCellForDropZones();
            CalculateTree();
        }

        private float2 GetCenterPosition(int2 gridPosition)
        {
            return BaseFlowField.GetCenterPosition(gridPosition, new int2(1, 1)).xy + new float2(-0.5f, -0.5f);
        }

        private void ChangeFlowField(GridPositionStruct gridPosition, bool isLockState, Cell[,] grid)
        {
            if (!mission.DropZoneCanInfluenceToFlowField)
                return;
            float2 startPosition = GetCenterPosition(gridPosition.GridPos);
            float2 leftPoint = startPosition + gridPosition.GridSize;

            int2 topLeftPoint = (int2)startPosition;
            int2 botRightPoint = (int2)leftPoint;

            for (int x = topLeftPoint.x; x < botRightPoint.x; x++)
            {
                for (int y = topLeftPoint.y; y < botRightPoint.y; y++)
                {
                    Cell cell = grid[x, y];
                    if (isLockState && !cell.IsWall)
                    {
                        cell.SetLockCost();
                    }
                    else if (cell.IsWall && !isLockState)
                    {
                        cell.SetDefaultCost();
                    }

                    grid[x, y] = cell;
                }
            }
        }

        private void DropZoneStateChanged(DropZoneComponent dropZoneComponent, GridPositionComponent grid, bool isOccupied)
        {
            // Debug.Log($"DropZoneStateChanged {mission.DropZoneCanInfluenceToFlowField} dropZoneComponent.IsCanInfluenceToFlowField {dropZoneComponent.IsCanInfluenceToFlowField}");

            if (!mission.DropZoneCanInfluenceToFlowField)
                return;

            if (!dropZoneComponent.IsCanInfluenceToFlowField)
                return;

            bool hasExit = !mission.CheckExits;

            ChangeFlowField(grid.Value, isOccupied, gridMap);
            CalculateTree();

            using NativeArray<DropZoneComponent> dropZonesComponents = dropZoneQuery.ToComponentDataArray<DropZoneComponent>(Allocator.Temp);
            using NativeArray<GridPositionComponent> dropZonesGrid = dropZoneQuery.ToComponentDataArray<GridPositionComponent>(Allocator.Temp);
            using NativeArray<Entity> dropZonesEntities = dropZoneQuery.ToEntityArray(Allocator.Temp);
            using NativeList<int> needCheckIndexes = new(Allocator.Temp);
            
            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            for (int i = 0; i < dropZonesComponents.Length; i++)
            {
                DropZoneComponent dropZone = dropZonesComponents[i];
                if (dropZone is {IsOccupied: false, IsCanInfluenceToFlowField: true})
                {
                    if (!dropZone.IsPossibleToBuild && isOccupied)
                        continue;

                    if (dropZone.IsPossibleToBuild && !isOccupied)
                        continue;

                    if (CheckForDeepChecker(dropZonesGrid[i], gridMap))
                    {
                        needCheckIndexes.Add(i);
                    } else if (!dropZone.IsPossibleToBuild)
                    {
                        dropZone.IsPossibleToBuild = true;
                        manager.SetComponentData(dropZonesEntities[i], dropZone);
                        Entity dropZoneEvent = manager.CreateEntity();
                        manager.SetName(dropZoneEvent, nameof(DropZoneEvent));
                        manager.AddComponentData(dropZoneEvent, new DropZoneEvent() { Entity = dropZonesEntities[i] });
                    }
                } else if (dropZone is {IsOccupied: true, IsCanInfluenceToFlowField: true, TimeToReactivate: > 0})
                {
                    needCheckIndexes.Add(i);
                }
            }

            foreach (int dropZoneIndex in needCheckIndexes)
            {
                DropZoneComponent dropZone = dropZonesComponents[dropZoneIndex];
                bool canBuild = CanBuildForThisDropZone(dropZonesGrid[dropZoneIndex], areas,hasExit);

                if (dropZone.IsPossibleToBuild == canBuild) continue;

                dropZone.IsPossibleToBuild = canBuild;
                manager.SetComponentData(dropZonesEntities[dropZoneIndex], dropZone);
                Entity dropZoneEvent = manager.CreateEntity();
                manager.SetName(dropZoneEvent, nameof(DropZoneEvent));
                manager.AddComponentData(dropZoneEvent, new DropZoneEvent() { Entity = dropZonesEntities[dropZoneIndex] });
            }
        }


        private bool CanBuildForThisDropZone(GridPositionComponent gridPosition, List<PairList> tmpAreas,bool hasExit)
        {
            float2 startPosition = GetCenterPosition(gridPosition.Value.GridPos);
            List<int2> removeList = new();
            for (int i = 0; i < gridPosition.Value.GridSize.x; i++)
            {
                for (int j = 0; j < gridPosition.Value.GridSize.y; j++)
                {
                    removeList.Add(new int2(i + (int)startPosition.x, j + (int)startPosition.y));
                }
            }

            for (int index = 0; index < tmpAreas.Count; index++)
            {
                PairList cloneArea = tmpAreas[index].Clone();

                foreach (int2 removeItem in removeList)
                {
                    cloneArea.Dictionary.Remove(removeItem);
                }

                if (cloneArea.Dictionary.Count == tmpAreas[index].Dictionary.Count)
                    continue;

                // Debug.Log("Need to check");

                return CanBuild(cloneArea, index,hasExit);
            }

            return true;
        }

        private bool CanBuild(PairList area, int index,bool hasExit)
        {
            HashSet<int2> spawnGroupCounter = new();

            HashSet<int2> tmpPos = new();
            HashSet<int2> checkPosition = new() {area.StartPosition};
            HashSet<int2> energyCoresCounter = new();

            while (checkPosition.Count > 0)
            {
                int2 pos = checkPosition.First();

                if (!tmpPos.Contains(pos))
                {
                    tmpPos.Add(pos);

                    if (energyCoresCount[index].Count > energyCoresCounter.Count)
                        if (energyCoresCount[index].Contains(pos))
                            energyCoresCounter.Add(pos);

                    if (countSpawnGroupsPerStartPoint[index].Count > spawnGroupCounter.Count)
                        foreach (SpawnGroup group in spawnGroups)
                        {
                            if (group.GetSpawnPositions().Contains(pos) && !spawnGroupCounter.Contains(group.GetSpawnPositions().ElementAt(0)))
                            {
                                spawnGroupCounter.Add(group.GetSpawnPositions().ElementAt(0));
                                break;
                            }
                        }

                    if (!hasExit && exitPoints.Contains(pos))
                        hasExit = true;

                    if (hasExit && countSpawnGroupsPerStartPoint[index].Count <= spawnGroupCounter.Count && energyCoresCount[index].Count <= energyCoresCounter.Count)
                    {
                        // Debug.Log($"countSpawnGroupsPerStartPoint[index].Count {countSpawnGroupsPerStartPoint[index].Count} spawnGroupCounter.Count {spawnGroupCounter.Count}");
                        // Debug.Log($"energyCoresCount[index].Count {energyCoresCount[index].Count} energyCoresCounter.Count {energyCoresCounter.Count}");
                        return true;
                    }

                    if (area.Dictionary.TryGetValue(pos, out HashSet<int2> dictPositions))
                        checkPosition.AddRange(dictPositions);
                }

                checkPosition.Remove(pos);
            }

            return false;
        }

        private bool CheckForDeepChecker(GridPositionComponent gridPositionComponent, Cell[,] map)
        {
            bool hasWall = false;
            int startWallIndex = 0;
            int endWallIndex = 0;
            bool findWall = false;
            int wallLenght = 0;

            int2 position;

            for (int i = 0; i < 12; i++)
            {
                if (i % 3 == 0)
                    continue;
                position = gridPositionComponent.Value.GridPos + positions[i];

                if (map[position.x, position.y].IsWall)
                {
                    if (hasWall)
                    {
                        endWallIndex = i;
                        wallLenght++;
                        continue;
                    }

                    if (findWall)
                    {
                        return true;
                    }

                    wallLenght = 1;
                    hasWall = true;
                    findWall = true;
                    startWallIndex = i;
                    endWallIndex = i;
                }
                else
                {
                    if (hasWall)
                        hasWall = false;
                }
            }

            if (!findWall)
            {
                int countWall = 0;
                for (int i = 0; i < 4; i++)
                {
                    position = gridPositionComponent.Value.GridPos + positions[i * 3];
                    if (map[position.x, position.y].IsWall)
                    {
                        countWall++;
                    }

                    if (countWall > 1)
                    {
                        return true;
                    }
                }
            }


            else if (wallLenght < 7)
            {
                position = gridPositionComponent.Value.GridPos + positions[0];
                if (startWallIndex > 1 && endWallIndex < 11 && map[position.x, position.y].IsWall)
                {
                    return true;
                }

                position = gridPositionComponent.Value.GridPos + positions[3];
                if ((startWallIndex > 4 || endWallIndex < 2) && map[position.x, position.y].IsWall)
                {
                    return true;
                }

                position = gridPositionComponent.Value.GridPos + positions[6];
                if ((startWallIndex > 7 || endWallIndex < 5) && map[position.x, position.y].IsWall)
                {
                    return true;
                }

                position = gridPositionComponent.Value.GridPos + positions[9];
                if ((startWallIndex > 10 || endWallIndex < 8) && map[position.x, position.y].IsWall)
                {
                    return true;
                }
            }

            return false;
        }

        private void CalculateTree()
        {
            areas.Clear();
            int height = gridMap.GetLength(1);
            int width = gridMap.GetLength(0);

            for (int index = 0; index < startPoints.Count; index++)
            {
                int2 startPoint = startPoints[index];

                countSpawnGroupsPerStartPoint[index].Clear();
                energyCoresCount[index].Clear();

                if (areas.Exists(pair => pair.Dictionary.ContainsKey(startPoint)))
                    continue;

                energyCoresCount[index].Add(startPoint);

                PairList area = new() {StartPosition = startPoint};
                HashSet<int2> checkPoints = new() {startPoint};

                while (checkPoints.Count > 0)
                {
                    int2 position = checkPoints.First();

                    foreach (Portal portal in portals)
                    {
                        TryAddPositionFromPortal(portal.Out.GridPos);

                        void TryAddPositionFromPortal(int2 portalPosition)
                        {
                            if (portalPosition.x > position.x || portalPosition.y > position.y)
                                return;
                            if (portalPosition.x + portal.Out.GridSize.x <= position.x || portalPosition.y + portal.Out.GridSize.y <= position.y)
                                return;

                            for (int pIndex = 0; pIndex < portal.Out.GridSize.x; pIndex++)
                            {
                                for (int j = 0; j < portal.Out.GridSize.y; j++)
                                {
                                    if (portalPosition.x + pIndex != position.x || portalPosition.y + j != position.y) continue;
                                    AddPositionToDictionary(ref area, position, portal.In.GridPos);
                                    checkPoints.Add(portal.In.GridPos);
                                    return;
                                }
                            }
                        }
                    }

                    if (position.x < width && position.x >= 0 && position.y < height && position.y >= 0)
                    {
                        TryAddPoint(-1, 0);
                        TryAddPoint(+1, 0);
                        TryAddPoint(0, 1);
                        TryAddPoint(0, -1);

                        void TryAddPoint(int stepX, int stepY)
                        {
                            int2 newPos = position + new int2(stepX, stepY);

                            if (checkPoints.Contains(newPos))
                                return;

                            if (newPos.x < 0 || newPos.x >= width || newPos.y < 0 || newPos.y >= height)
                                return;

                            if (gridMap[newPos.x, newPos.y].IsWall)
                                return;

                            if (area.Dictionary.ContainsKey(newPos) && area.Dictionary[newPos].Contains(position))
                                return;

                            // if (area.Dictionary.ContainsKey(position) && area.Dictionary[position].Contains(newPos))
                            //     return;

                            foreach (SpawnGroup group in spawnGroups)
                            {
                                HashSet<int2> points = group.GetSpawnPositions();
                                if (!points.Contains(newPos)) continue;

                                if (!countSpawnGroupsPerStartPoint[index].Contains(points.First()))
                                    countSpawnGroupsPerStartPoint[index].Add(points.First());
                                break;
                            }

                            if (startPoints.Contains(newPos)) //&& !energyCoresCount[index].Contains(newPos)
                                energyCoresCount[index].Add(newPos);

                            AddPositionToDictionary(ref area, position, newPos);

                            checkPoints.Add(newPos);
                        }
                    }

                    checkPoints.Remove(position);
                }

                areas.Add(area);
                // Debug.Log($"area count: {area.Dictionary.Count}");
                // Debug.Log($"energyCoresCount count {energyCoresCount[index].Count}");
                // Debug.Log($"countSpawnGroupsPerStartPoint count {countSpawnGroupsPerStartPoint[index].Count}");
            }
        }

        private void AddPositionToDictionary(ref PairList pairList, int2 currentPos, int2 newPos)
        {
            if (!pairList.Dictionary.ContainsKey(currentPos))
                pairList.Dictionary.Add(currentPos, new HashSet<int2>());

            if (!pairList.Dictionary[currentPos].Contains(newPos))
                pairList.Dictionary[currentPos].Add(newPos);

            if (!pairList.Dictionary.ContainsKey(newPos))
                pairList.Dictionary.Add(newPos, new HashSet<int2>());

            if (!pairList.Dictionary[newPos].Contains(currentPos))
                pairList.Dictionary[newPos].Add(currentPos);
        }
    }
}