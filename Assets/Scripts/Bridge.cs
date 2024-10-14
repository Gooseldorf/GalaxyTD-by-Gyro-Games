using System;
using Unity.Mathematics;
using UnityEngine;
using static MusicManager;

[Serializable]
public class Bridge : IGridPosition, IPowerable
{
    [field: SerializeField] public int Id { get; set; }
    [field: SerializeField] public int2 GridPos { get; set; }
    [field:SerializeField] public int2 GridSize { get; set; }
   
    [field:SerializeField] public bool IsPowered { get; set; }
    float3 IPosition.Direction { get; set; }
    
    public event Action OnTogglePower;

    public void TogglePower()
    {
        IsPowered = !IsPowered;
        OnTogglePower?.Invoke();
        PlaySound2D(IsPowered ? SoundKey.Element_activated : SoundKey.Element_deactivated);
    }

    public GridPosition GetTotalGridPosition()
    {
        int2 gridPos;
        int2 gridSize;
        if (GridSize.x > 2)
        {
            gridPos = new int2(GridPos.x, GridPos.y - 1);
            gridSize = new int2(GridSize.x, GridSize.y + 2);
        }
        else
        {
            gridPos = new int2(GridPos.x - 1, GridPos.y);
            gridSize = new int2(GridSize.x + 2, GridSize.y);
        }

        return new GridPosition(gridPos, gridSize);
    }
}