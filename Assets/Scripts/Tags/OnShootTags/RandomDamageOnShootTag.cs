using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public class RandomDamageOnShootTag : OnShootTag
{
    [SerializeField, InfoBox("Minimum damage percent bound. 100% is 1")] private float minDamagePercent = 0.01f;
    [SerializeField, InfoBox("Maximum damage percent bound. 100% is 1")] private float maxDamagePercent = 10f;
    
    public override void OnShoot(Entity tower, Entity shootEntity, EntityCommandBuffer ecb, EntityManager manager, DynamicBuffer<EntitiesBuffer> dynamicBuffer)
    {
        foreach (Entity entity in dynamicBuffer)
        {
            ProjectileComponent projectileComponent = manager.GetComponentData<ProjectileComponent>(entity);
            
            float randPercent = Random.Range(minDamagePercent, maxDamagePercent);
            float randomizedDamage = projectileComponent.Damage * randPercent;
            projectileComponent.Damage = randomizedDamage;
            
            manager.SetComponentData(entity, projectileComponent);
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/RandomDamage")
                                                .Replace("{param2}", (minDamagePercent * 100).ToString())
                                                .Replace("{param1}", (maxDamagePercent * 100).ToString());
}