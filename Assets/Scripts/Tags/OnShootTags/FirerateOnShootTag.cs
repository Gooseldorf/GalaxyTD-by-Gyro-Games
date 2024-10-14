using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class FirerateOnShootTag : OnShootTag
{
    [SerializeField, InfoBox("Percent from AttackSpeed. 100% is 1. Positive number for increase AttackSpeed, negative decrease")] private float changeFireratePercent = .01f;
    
    public override void OnShoot(Entity tower, Entity shootEntity, EntityCommandBuffer ecb, EntityManager manager, DynamicBuffer<EntitiesBuffer> dynamicBuffer)
    {
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
        attackerComponent.AttackStats.ShootingStats.ShotDelay /= (1 + changeFireratePercent);
        
        manager.SetComponentData(tower, attackerComponent);
    }
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/FirerateOnShoot");
}