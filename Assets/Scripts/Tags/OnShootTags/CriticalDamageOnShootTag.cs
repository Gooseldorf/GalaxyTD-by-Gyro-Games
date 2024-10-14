using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using System;
using Unity.Entities;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class CriticalDamageOnShootTag : OnShootTag
{
    [SerializeField, InfoBox("Percents"), Range(0, 100)] private float critChance = 25f;
    [SerializeField, InfoBox("Bullet damage multiplier")] private float critAmount;

    public override void OnShoot(Entity tower, Entity shootEntity, EntityCommandBuffer ecb, EntityManager manager, DynamicBuffer<EntitiesBuffer> dynamicBuffer)
    {
        float rand = Random.Range(0f, 100f);

        if (rand <= critChance)
        {
            AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
            MuzzleTimedEvent shootEvent = manager.GetComponentData<MuzzleTimedEvent>(shootEntity);
            
            foreach (Entity entity in dynamicBuffer)
                SetDamage(attackerComponent, entity, manager, critAmount, ref shootEvent);
            
            manager.SetComponentData(shootEntity, shootEvent);
        }
    }

    private void SetDamage(AttackerComponent attackerComponent, Entity projectileEntity, EntityManager manager, float crit, ref MuzzleTimedEvent shootEvent)
    {
        switch (attackerComponent.TowerType)
        {
            case AllEnums.TowerId.Mortar:
                MortarProjectile mortarProjectile = manager.GetComponentData<MortarProjectile>(projectileEntity);
                mortarProjectile.Damage *= crit;
                mortarProjectile.IsEnhanced = shootEvent.IsEnhanced = true;
                manager.SetComponentData(projectileEntity, mortarProjectile);
                break;
            case AllEnums.TowerId.Rocket:
                RocketProjectile rocketProjectile = manager.GetComponentData<RocketProjectile>(projectileEntity);
                rocketProjectile.Damage *= crit;
                rocketProjectile.IsEnhanced = shootEvent.IsEnhanced = true;
                manager.SetComponentData(projectileEntity, rocketProjectile);
                break;
            default:
                ProjectileComponent projectileComponent = manager.GetComponentData<ProjectileComponent>(projectileEntity);
                projectileComponent.Damage *= crit;
                projectileComponent.IsEnhanced = shootEvent.IsEnhanced = true;
                manager.SetComponentData(projectileEntity, projectileComponent);
                break;
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/CriticalDamage")
                                                .Replace("{param1}", critChance + "<color=#1fb2de>%</color>")
                                                .Replace("{param2}", critAmount.ToString());
}