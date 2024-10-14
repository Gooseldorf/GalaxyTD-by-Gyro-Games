using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

[Serializable]
public class SpawnGroup
{
    public GridPosition[] SpawnPositions;
    public List<GridPosition> CombinedZones = new();
    [OdinSerialize, NonSerialized] public List<Wave> Waves;

    private HashSet<int2> spawnPositions = new();

    public HashSet<int2> GetSpawnPositions()
    {
        if (spawnPositions == null)
            spawnPositions = new();

        if (spawnPositions.Count == 0)
        {
            foreach (GridPosition gridPosition in SpawnPositions)
                for (int i = 0; i < gridPosition.GridSize.x; i++)
                for (int j = 0; j < gridPosition.GridSize.y; j++)
                    spawnPositions.Add(new int2(gridPosition.GridPos.x + i, gridPosition.GridPos.y + j));
        }

        return spawnPositions;
    }
}