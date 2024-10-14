using System;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class GridPosition : IGridPosition
{
    public GridPosition(int2 gridPos, int2 gridSize)
    {
        GridPos = gridPos;
        GridSize = gridSize;
    }

    [field: SerializeField] public int2 GridPos { get; set; }
    [field: SerializeField] public int2 GridSize { get; set; }

    public int Area => GridSize.x * GridSize.y;
    public float3 Direction { get; set; }
}
