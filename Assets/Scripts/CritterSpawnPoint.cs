using System;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class CritterSpawnPoint : IGridPosition
{
    [field: SerializeField] public CritterStats CritterStats;
    
    public float3 Direction { get; set; }
    [field: SerializeField] public int2 GridPos { get; set; }
    [field: SerializeField] public int2 GridSize { get; set; }
}
