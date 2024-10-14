using Unity.Entities;
using Unity.Mathematics;

public class RuntimeSpawnData
{
    public CreepStats CreepStats;
    public float HpModifier;
    public float TimeOfSpawn;
    public int2 GridCoordinate;
}

public struct RuntimeSpawnDataStruct : IComponentData
{
    public Entity Entity;
    
    public CreepStatsConfig CreepStatsConfig;
    public float HpModifier;
    public float TimeOfSpawn;
    public int2 GridCoordinate;
}

