using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class DropZone : IGridPosition, IPowerable, ICloneable
{
    [field: SerializeField] public int Id { get; set; }
    
    [field: SerializeField] public int2 GridPos { get; private set; }
    [field: SerializeField] public int2 GridSize { get; private set; }
    [field: SerializeField] public bool IsPowered { get; private set; }
    
    [field: SerializeField] public bool IsCanInfluenceToFlowField { get; private set; }


    public event Action OnTogglePower;

    [ShowInInspector]
    public Tower Tower { get; }
    [ShowInInspector]
    public bool IsOccupied => Tower != null;
    [ShowInInspector]
    public bool BlocksMovement { get; private set; } = false;
    
    public float3 Direction { get; set; }
    
    public void TogglePower()
    {
        IsPowered = !IsPowered;
        OnTogglePower?.Invoke();
    }
    
    public object Clone()
    {
        DropZone result = this.MemberwiseClone() as DropZone;
        return result;
    }

    public DropZone(int2 pos, int2 size, int id, bool isPowered,bool isCanInfluenceToFlowField)
    {
        GridPos = pos;
        GridSize = size;
        Id = id;
        IsPowered = isPowered;
        IsCanInfluenceToFlowField = isCanInfluenceToFlowField;
    }
}