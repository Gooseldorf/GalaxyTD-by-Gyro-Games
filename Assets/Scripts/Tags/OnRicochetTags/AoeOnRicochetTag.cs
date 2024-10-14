using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class AoeOnRicochetTag : OnRicochetTag
{
    [SerializeField] private float range = 5f;
    [SerializeField, InfoBox("Percent from damage. 100% is 1")] private float damagePercent = 1f;
    [SerializeField, InfoBox("Percent from attacker knockback. 100% is 1")] private float knockbackPercent = 1f;
    
    public override void OnRicochet(ProjectileComponent projectileComponent, PositionComponent positionComponent, RefRW<CashComponent> cashComponent, EntityManager manager, EntityCommandBuffer ecb)
    {
        if (!manager.Exists(projectileComponent.AttackerEntity))
        {
            Debug.LogError("Attacker entity doesn't exist => sold tower more than 4 seconds ago ?");
            return;
        }
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(projectileComponent.AttackerEntity);
        float aoeDamage = projectileComponent.Damage * damagePercent;
        float knockback = attackerComponent.AttackStats.KnockBackPerBullet * knockbackPercent;
        
        manager.CompleteDependencyBeforeRW<CreepsLocator>();
        EntityQuery creepsLocatorQuery = manager.CreateEntityQuery(new ComponentType[] {typeof(CreepsLocator)});
        CreepsLocator creepsLocator = creepsLocatorQuery.GetSingleton<CreepsLocator>();
        
        DamageSystem.DoTagAoeDamage(creepsLocator, positionComponent.Position, range, aoeDamage, knockback, projectileComponent.AttackerEntity, manager, cashComponent, ecb);        
        //TODO need something like event for aoe visual
    }
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/AoeOnRicochet");
}