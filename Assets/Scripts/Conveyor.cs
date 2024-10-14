using System;
using Unity.Mathematics;
using UnityEngine;
using static MusicManager;

public class Conveyor: IGridPosition, IPowerable, ICloneable
{
    public int2 ConveyorDirection;
    public float Speed;
    public float3 Direction { get; set; }
    [field: SerializeField] public int2 GridPos { get; }
    [field: SerializeField]public int2 GridSize { get; }
    [field: SerializeField] public int Id { get; set; }
    [field: SerializeField] public bool IsPowered { get; set; }
    
    public event Action OnTogglePower;
    public void TogglePower()
    {
        IsPowered = !IsPowered;
        OnTogglePower?.Invoke();
        PlaySound2D(IsPowered ? SoundKey.Element_activated : SoundKey.Element_deactivated);
    }

    public Conveyor(int2 gridPosition, int2 gridSize, int2 direction, int id, bool isPowered, float speed)
    {
        GridPos = gridPosition;
        GridSize = gridSize;
        ConveyorDirection = direction;
        Id = id;
        IsPowered = isPowered;
        Speed = speed;
    }

    public object Clone()
    {
        Conveyor result = this.MemberwiseClone() as Conveyor;
        return result;
    }
}
