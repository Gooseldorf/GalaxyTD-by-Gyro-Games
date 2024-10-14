using CardTD.Utilities;
using Unity.Entities;
using Unity.Mathematics;
using static AllEnums;
using static RocketTargetingSystem;

public struct RocketProjectile : IComponentData,IEnableableComponent
{
    public Entity Tower;
    public float Damage;
    public float AOE;

    public float2 OriginPoint;
    public float2 OffsetPoint;
    public float2 Target;

    public float TotalFlyTime;
    public float PathProgress;

    public bool IsLastBullet;
    public bool IsEnhanced;

    public RocketProjectile(ShootData shootData, float2 offsetPoint, float2 target, bool isLastBullet, bool isEnhanced)
    {
        OriginPoint = shootData.Origin;
        OffsetPoint = offsetPoint;
        Target = target;
        TotalFlyTime = Utilities.GetBezierLength(OriginPoint, OffsetPoint, Target) / shootData.ProjectileSpeed;
        PathProgress = shootData.InitialFlyTime / TotalFlyTime;
        Tower = shootData.Tower;
        Damage = shootData.Damage;
        AOE = shootData.AOE;
        IsLastBullet = isLastBullet;
        IsEnhanced = isEnhanced;
    }


    public float2 GetPosition()
    {
        float2 tempFrom = math.lerp(OriginPoint, OffsetPoint, PathProgress);
        float2 tempTo = math.lerp(OffsetPoint, Target, PathProgress);
        float2 position = math.lerp(tempFrom, tempTo, PathProgress);

        return position;
    }

    public void SetFlyTime(float projectileSpeed)
    {
        TotalFlyTime = Utilities.GetBezierLength(OriginPoint, OffsetPoint, Target) / projectileSpeed;
    }
}
