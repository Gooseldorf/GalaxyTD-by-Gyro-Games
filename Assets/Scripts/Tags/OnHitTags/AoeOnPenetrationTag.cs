using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class AoeOnPenetrationTag : OnHitTag
{
    [SerializeField] private float range = 5f;
    [SerializeField] private int minPenetrationCount = 1;
    [SerializeField, InfoBox("Percent from damagePerBullet. 100% is 1")] private float damagePercent = 1f;
    [SerializeField, InfoBox("Percent from attacker knockback. 100% is 1")] private float knockbackPercent = 1f;
    
    public override void OnHit(OnHitTagData onHitTagData, ref CreepComponent creepComponent, ref float damage, Entity tower, EntityManager manager, EntityCommandBuffer ecb)
    {
        if (onHitTagData.GunCollisionEvent.ProjectileComponent.PenetrationCount >= minPenetrationCount)
        {
            if(!manager.Exists(onHitTagData.CreepEntity))
                return;
            
            AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
            float aoeDamage = damage * damagePercent;
            float knockback = attackerComponent.AttackStats.KnockBackPerBullet * knockbackPercent;
            
            manager.CompleteDependencyBeforeRW<CreepsLocator>();
            EntityQuery creepsLocatorQuery = manager.CreateEntityQuery(new ComponentType[] {typeof(CreepsLocator)});
            CreepsLocator creepsLocator = creepsLocatorQuery.GetSingleton<CreepsLocator>();
            PositionComponent creepPositionComponent = manager.GetComponentData<PositionComponent>(onHitTagData.CreepEntity);
            DamageSystem.DoTagAoeDamage(creepsLocator, creepPositionComponent.Position, range, aoeDamage, knockback, tower, manager, onHitTagData.CashComponentRefRw, ecb);
            
            //TODO need something like event for aoe visual
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/AoeOnPenetration");
}