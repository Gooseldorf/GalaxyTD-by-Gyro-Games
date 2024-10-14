using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct KnockBackWallDamageEvent : IComponentData,IEnableableComponent
{
    public Entity OriginTower;
    public Entity Creep;
    public float2 Position;
    /// <summary>
    /// This affects Damage Recieved
    /// </summary>
    public float Speed;
}
