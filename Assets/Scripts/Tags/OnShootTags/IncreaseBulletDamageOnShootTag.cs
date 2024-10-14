using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class IncreaseBulletDamageOnShootTag : OnShootTag
{
    [SerializeField, InfoBox("Increase damage of every Nth bullet")] private int bulletsCount = 6;
    [SerializeField, InfoBox("Percent from bullet Damage. 100% is 1. Positive numbers to increase bullet Damage, negative decrease")] private float increasePercent = .5f;

    public override void OnShoot(Entity tower, Entity shootEntity, EntityCommandBuffer ecb, EntityManager manager, DynamicBuffer<EntitiesBuffer> dynamicBuffer)
    {
        AttackerStatisticComponent statistics = manager.GetComponentData<AttackerStatisticComponent>(tower);

        if (statistics.Shoots % bulletsCount != 0) return;

        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
        
        foreach (Entity projectileEntity in dynamicBuffer)
        {
            MuzzleTimedEvent shootEvent = manager.GetComponentData<MuzzleTimedEvent>(shootEntity);
            SetSixthBullet(attackerComponent, manager, projectileEntity, ref shootEvent);
            manager.SetComponentData(shootEntity, shootEvent);
        }
    }

    private void SetSixthBullet(AttackerComponent attackerComponent, EntityManager manager, Entity projectileEntity, ref MuzzleTimedEvent shootEvent)
    {
         switch (attackerComponent.TowerType)
        {
            case AllEnums.TowerId.Mortar:
                MortarProjectile mortarProjectile = manager.GetComponentData<MortarProjectile>(projectileEntity);
                mortarProjectile.Damage += mortarProjectile.Damage * increasePercent;
                mortarProjectile.IsEnhanced = shootEvent.IsEnhanced = true;
                manager.SetComponentData(projectileEntity, mortarProjectile);
                break;
            
            case AllEnums.TowerId.Rocket:
                RocketProjectile rocketProjectile = manager.GetComponentData<RocketProjectile>(projectileEntity);
                rocketProjectile.Damage += rocketProjectile.Damage * increasePercent;
                rocketProjectile.IsEnhanced = shootEvent.IsEnhanced = true;
                manager.SetComponentData(projectileEntity, rocketProjectile);
                break;
            
            default:
                ProjectileComponent projectileComponent = manager.GetComponentData<ProjectileComponent>(projectileEntity);
                projectileComponent.Damage += projectileComponent.Damage * increasePercent;
                projectileComponent.IsEnhanced = shootEvent.IsEnhanced = true;
                manager.SetComponentData(projectileEntity, projectileComponent);
                break;
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/Shooter")
                                                .Replace("{param1}", bulletsCount.ToString())
                                                .Replace("{param2}", (increasePercent + 1).ToString());
}