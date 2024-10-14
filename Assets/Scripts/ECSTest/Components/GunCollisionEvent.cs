using System;
using Unity.Entities;
using Unity.Mathematics;
using static AllEnums;

namespace ECSTest.Components
{
    public struct GunCollisionEvent : IComponentData, IComparable<GunCollisionEvent>,IEnableableComponent
    {
        public TowerId TowerId;
        public FleshType FleshType;
        public ArmorType ArmorType;
        public ProjectileComponent ProjectileComponent;
        public Entity Target;
        public Entity Tower;
        public float2 CollisionDirection;
        public float2 Point;
        public float Damage;
        public float DistanceTraveled;
        public int SortCollisionIndex;
        public bool IsEnhanced;

        public int CompareTo(GunCollisionEvent other) => SortCollisionIndex.CompareTo(other.SortCollisionIndex);
    }

    public struct CollisionObstacleEvent : IComponentData, IEnableableComponent
    {
        public TowerId TowerId;
        public ProjectileComponent ProjectileComponent;
        public Entity Tower;
        public float2 Point;
        public float2 CollisionDirection;
        public float2 Normal;
    }


    public struct AOECollisionEvent : IComponentData,IEnableableComponent
    {
        public TowerId TowerId;
        public Entity Tower;
        public float2 Point;
        public float Damage;
        public float AOE;
        public bool IsLastBulletProjectile;

        public AOECollisionEvent(RocketProjectile rocketProjectile)
        {
            TowerId = TowerId.Rocket;
            Tower = rocketProjectile.Tower;
            Point = rocketProjectile.Target;
            Damage = rocketProjectile.Damage;
            AOE = rocketProjectile.AOE;
            IsLastBulletProjectile = rocketProjectile.IsLastBullet;
        }

        public AOECollisionEvent(MortarProjectile projectile)
        {
            TowerId = TowerId.Mortar;
            Tower = projectile.Tower;
            Point = projectile.Target;
            Damage = projectile.Damage;
            AOE = projectile.AOE;
            IsLastBulletProjectile = projectile.IsLastBullet;
        }
    }
}