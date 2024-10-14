using System;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct StaticPosition : IPosition
{
    [field: SerializeField] public float3 Position { get; set; }
    [field: SerializeField] public float3 Direction { get; set; }

    public static implicit operator float3(StaticPosition staticPosition) => staticPosition.Position;

    public static implicit operator StaticPosition(float3 float3) => new StaticPosition {Position = float3};

    public StaticPosition(float3 position)
    {
        Position = position;
        Direction = float3.zero;
    }

    public static IPosition IPositionToStaticPoint(IPosition position)
    {
        return new StaticPosition {Position = position.Position};
    }
}