using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class IncreaseLastBulletDamageTag : OnShootTag
{
    [SerializeField, InfoBox("Percent from MagazineSize. 100% is 1.")] private float percent = .25f;
    
    public override void OnShoot(Entity tower, Entity shootEntity, EntityCommandBuffer ecb, EntityManager manager, DynamicBuffer<EntitiesBuffer> dynamicBuffer)
    {
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
        
        foreach (Entity entity in dynamicBuffer)
        {
            MuzzleTimedEvent shootEvent = manager.GetComponentData<MuzzleTimedEvent>(shootEntity);
            SetLastBullet(attackerComponent, entity, manager, ref shootEvent);
            manager.SetComponentData(shootEntity, shootEvent);
        }
    }

    private void SetLastBullet(AttackerComponent attackerComponent, Entity projectileEntity, EntityManager manager, ref MuzzleTimedEvent shootEvent)
    {
        switch (attackerComponent.TowerType)
        {
            case AllEnums.TowerId.Mortar:
                MortarProjectile mortarProjectile = manager.GetComponentData<MortarProjectile>(projectileEntity);
                if (!mortarProjectile.IsLastBullet) return;
                
                float increaseDamageValueMortar = (attackerComponent.AttackStats.ReloadStats.MagazineSize * attackerComponent.AttackStats.DamagePerBullet) * percent;
                mortarProjectile.Damage += increaseDamageValueMortar;
                mortarProjectile.IsEnhanced = shootEvent.IsEnhanced = true;
                manager.SetComponentData(projectileEntity, mortarProjectile);
                break;
            
            case AllEnums.TowerId.Rocket:
                RocketProjectile rocketProjectile = manager.GetComponentData<RocketProjectile>(projectileEntity);
                if (!rocketProjectile.IsLastBullet) return;
                
                float increaseDamageValueRocket = (attackerComponent.AttackStats.ReloadStats.MagazineSize * attackerComponent.AttackStats.DamagePerBullet) * percent;
                rocketProjectile.Damage += increaseDamageValueRocket;
                rocketProjectile.IsEnhanced = shootEvent.IsEnhanced = true;
                manager.SetComponentData(projectileEntity, rocketProjectile);
                break;
            
            default:
                ProjectileComponent projectileComponent = manager.GetComponentData<ProjectileComponent>(projectileEntity);
                if (!projectileComponent.IsLastBullet) return;
                
                float increaseDamageValue = (attackerComponent.AttackStats.ReloadStats.MagazineSize * attackerComponent.AttackStats.DamagePerBullet) * percent;
                projectileComponent.Damage += increaseDamageValue;
                projectileComponent.IsEnhanced = shootEvent.IsEnhanced = true;
                manager.SetComponentData(projectileEntity, projectileComponent);
                break;
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/LastBulletDamage");
}