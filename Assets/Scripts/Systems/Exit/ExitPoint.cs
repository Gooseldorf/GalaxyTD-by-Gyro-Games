using System;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class ExitPoint : IGridPosition, /*IIdentifiable,*/ ICloneable
{
    //[field: SerializeField] public int Id { get; set; }
    [field: SerializeField] public int2 GridPos { get; private set; }
    [field: SerializeField] public int2 GridSize { get; private set; }
    public object Clone() => this.MemberwiseClone();
    public float3 Direction { get; set; }

    public bool IsCombinedZone;

    public ExitPoint(int2 pos, int2 size, /*int id,*/ bool isCombinedZone)
    {
        GridPos = pos;
        GridSize = size;
        //Id = id;
        IsCombinedZone = isCombinedZone;
    }
}