using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct ProjectileComponent : IComponentData, IEnableableComponent
    {
        public Entity AttackerEntity;

        public ProjectileComponent(Entity attackerEntity, AttackerComponent attackerComponent, GunStatsComponent gunStats, float flyTime, bool isLastBullet, bool isEnhanced,float2 startPosition)
        {
            AttackerEntity = attackerEntity;
            Velocity = attackerComponent.AttackStats.ProjectileSpeed;
            Damage = attackerComponent.AttackStats.DamagePerBullet;
            TowerId = attackerComponent.TowerType;
            //TODO: Initial distance traveled
            bool isToPosition = attackerComponent.TowerType == AllEnums.TowerId.Mortar || attackerComponent.TowerType == AllEnums.TowerId.Rocket;
            DistanceTraveled = isToPosition? 0 : attackerComponent.StartOffset; //attackerComponent.StartOffset; NickS => check in othercases
            StartDistance = flyTime * Velocity + DistanceTraveled;
            RicochetCount = gunStats.RicochetCount;
            PenetrationCount = gunStats.PenetrationCount;
            DamageMultPerPenetration = gunStats.DamageMultPerPenetration;
            DamageMultPerRicochet = gunStats.DamageMultPerRicochet;
            IsLastBullet = isLastBullet;
            IsEnhanced = isEnhanced;
            StartPosition = startPosition;
        }

        public AllEnums.TowerId TowerId;
        public float2 StartPosition;
        public float Velocity;
        public float DistanceTraveled;
        public float Damage;
        public float StartDistance;

        public float DamageMultPerPenetration;
        public float DamageMultPerRicochet;

        public int RicochetCount;
        public int PenetrationCount;
        public bool IsLastBullet;
        public bool IsEnhanced;
    }
}