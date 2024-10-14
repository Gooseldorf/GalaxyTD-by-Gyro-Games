using Unity.Entities;
using Unity.Mathematics;

public struct MortarProjectile : IComponentData
{
    public Entity Tower;
    public float Damage;
    public float AOE;

    public float2 Target;

    public float RemainingTime;

    public bool IsLastBullet;
    public bool IsEnhanced;

    public MortarProjectile(Entity tower, float damage, float2 position, float flyTime, float aoe, bool isLastBullet, bool isEnhanced)
    {
        Tower = tower;
        Damage = damage;
        Target = position;
        RemainingTime = flyTime;
        AOE = aoe;
        IsLastBullet = isLastBullet;
        IsEnhanced = isEnhanced;
    }
}