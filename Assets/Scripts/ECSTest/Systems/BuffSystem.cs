using ECSTest.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ECSTest.Systems
{
    [UpdateAfter(typeof(MovingSystem))]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct BuffSystem : ISystem
    {
        private EntityQuery towerQuery;
        
        public void OnCreate(ref SystemState state)
        {
            towerQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AttackerComponent,DestroyComponent>()
                .Build(ref state);
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityManager manager = state.EntityManager;
            NativeArray<Entity> entities = towerQuery.ToEntityArray(Allocator.Temp);
            NativeArray<DestroyComponent> destroyComponents = towerQuery.ToComponentDataArray<DestroyComponent>(Allocator.Temp);

            for (int j = 0; j < entities.Length; j++)
            {
                if(destroyComponents[j].IsNeedToDestroy)
                    continue;
                
                Entity tower = entities[j];
                DynamicBuffer<BuffBuffer> buffer = manager.GetBuffer<BuffBuffer>(tower);

                for (int i = 0; i < buffer.Length; i++)
                {
                    BuffBuffer buff = buffer[i];
                    if (buff.Timer > 0)
                    {
                        buff.Timer -= SystemAPI.Time.DeltaTime;
                        buffer[i] = buff;
                    }
                    else
                    {
                        ReturnBuffValues(tower, manager, buff);
                        buffer.RemoveAt(i);
                    }
                }
            }

            destroyComponents.Dispose();
            entities.Dispose();
        }

        private void ReturnBuffValues(Entity tower, EntityManager manager, BuffBuffer buff)
        {
            if (buff.Type is (int)AllEnums.BuffType.Penetration or (int)AllEnums.BuffType.Ricochet)
            {
                if (manager.HasComponent<GunStatsComponent>(tower))
                {
                    GunStatsComponent gunStatsComponent = manager.GetComponentData<GunStatsComponent>(tower);

                    switch (buff.Type)
                    {
                        case (int)AllEnums.BuffType.Penetration:
                            gunStatsComponent.PenetrationCount -= (int)buff.BuffValue;
                            break;
                        case (int)AllEnums.BuffType.Ricochet:
                            gunStatsComponent.RicochetCount -= (int)buff.BuffValue;
                            break;
                    }
                    manager.SetComponentData(tower, gunStatsComponent);
                }
            }
            else
            {
                AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);

                switch (buff.Type)
                {
                    case (int)AllEnums.BuffType.Damage:
                        attackerComponent.AttackStats.DamagePerBullet -= buff.BuffValue;
                        break;
                    case (int)AllEnums.BuffType.Firerate:
                        attackerComponent.AttackStats.ShootingStats.ShotDelay += buff.BuffValue;
                        break;
                    case (int)AllEnums.BuffType.Range:
                        attackerComponent.AttackStats.AimingStats.Range -= buff.BuffValue;
                        break;
                    case (int)AllEnums.BuffType.ReloadSpeed:
                        attackerComponent.AttackStats.ReloadStats.ReloadTime += buff.BuffValue;
                        break;
                }
                manager.SetComponentData(tower, attackerComponent);
            }
        }

        public static void HandleBuff(AllEnums.BuffType buffType, float bonusValue, ref AttackerComponent component, out float bonusStat)
        {
            switch (buffType)
            {
                case AllEnums.BuffType.Damage:
                    bonusStat = component.AttackStats.DamagePerBullet * bonusValue;
                    component.AttackStats.DamagePerBullet += bonusStat;
                    break;
                case AllEnums.BuffType.Firerate:
                    float oldShotDelay = component.AttackStats.ShootingStats.ShotDelay;
                    bonusStat = oldShotDelay - (oldShotDelay / (1 + bonusValue));
                    component.AttackStats.ShootingStats.ShotDelay -= bonusStat;
                    break;
                case AllEnums.BuffType.Range:
                    bonusStat = component.AttackStats.AimingStats.Range * bonusValue;
                    component.AttackStats.AimingStats.Range += bonusStat;
                    break;
                case AllEnums.BuffType.ReloadSpeed:
                    float oldReloadTime = component.AttackStats.ReloadStats.ReloadTime;
                    bonusStat = oldReloadTime - (oldReloadTime / (1 + bonusValue));
                    component.AttackStats.ReloadStats.ReloadTime -= bonusStat;
                    break;
                default:
                    bonusStat = 0;
                    break;
            }
        }
        
        public static void HandleBuff(AllEnums.BuffType buffType, float bonusValue, ref GunStatsComponent component, out float bonusStat)
        {
            switch (buffType)
            {
                case AllEnums.BuffType.Penetration:
                    bonusStat = bonusValue;
                    component.PenetrationCount += (int)bonusStat;
                    break;
                case AllEnums.BuffType.Ricochet:
                    bonusStat = bonusValue;
                    component.RicochetCount += (int)bonusStat;
                    break;
                default:
                    bonusStat = 0;
                    break;
            }
        }
    }
}