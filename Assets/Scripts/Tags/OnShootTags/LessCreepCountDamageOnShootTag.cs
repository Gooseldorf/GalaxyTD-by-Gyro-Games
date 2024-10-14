using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class LessCreepCountDamageOnShootTag : OnShootTag
{
    [SerializeField, BoxGroup("Bounds")] private int minCreepsBound = 10;
    [SerializeField, BoxGroup("Bounds")] private int maxCreepsBound = 100;
    [SerializeField, BoxGroup("Bounds")] private float minPercentBound = 2f;
    [SerializeField, BoxGroup("Bounds")] private float maxPercentBound = .5f;
    
    public override void OnShoot(Entity tower, Entity shootEntity, EntityCommandBuffer ecb, EntityManager manager, DynamicBuffer<EntitiesBuffer> dynamicBuffer)
    {
        manager.CompleteDependencyBeforeRW<CreepsLocator>();
        EntityQuery query = manager.CreateEntityQuery(new ComponentType[] {typeof(CreepsLocator)});
        CreepsLocator creepsLocator = query.GetSingleton<CreepsLocator>();

        float damagePercent = Utilities.GetLerpedValue(minCreepsBound, maxCreepsBound, minPercentBound, maxPercentBound, creepsLocator.CreepHashMap.Count);
        
        foreach (Entity entity in dynamicBuffer)
        {
            ProjectileComponent projectileComponent = manager.GetComponentData<ProjectileComponent>(entity);

            projectileComponent.Damage *= (1 + damagePercent);

            manager.SetComponentData(entity, projectileComponent);
        }
    }
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/LessCreepDamage");
}