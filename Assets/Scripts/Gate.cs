using ECSTest.Structs;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static MusicManager;

[Serializable]
public class Gate : IGridPosition, IPowerable
{
    [field: SerializeField] public int Id { get; set; }
    [field: SerializeField] public int2 GridPos { get; set; }
    [field: SerializeField] public int2 GridSize { get; set; }
    float3 IPosition.Direction { get; set; }
    [field: SerializeField] public bool IsPowered { get; set; }

    public event Action OnTogglePower;

    public void TogglePower()
    {
        IsPowered = !IsPowered;
        OnTogglePower?.Invoke();
        PlaySound2D(IsPowered ? SoundKey.Element_activated : SoundKey.Element_deactivated);
    }

    public GridPositionStruct GetGrid()
    {
        return new GridPositionStruct {GridPos = this.GridPos, GridSize = this.GridSize};
    }

    public GridPositionStruct GetCentralPart()
    {
        GridPositionStruct grid = GetGrid();

        if (GridSize.x > 1)
        {
            grid.GridPos.x += 1;
            grid.GridSize.x -= 2;
        }
        else
        {
            grid.GridPos.y += 1;
            grid.GridSize.y -= 2;
        }

        return grid;
    }

    public List<GridPositionStruct> GetBoundaryPoints()
    {
        List<GridPositionStruct> positions = new();
        int2 one = new int2(1, 1);
        positions.Add(new GridPositionStruct(GridPos, one));
        int2 endPosition = GridPos + GridSize;
        positions.Add(new GridPositionStruct(endPosition - one, one));
        return positions;
    }
}