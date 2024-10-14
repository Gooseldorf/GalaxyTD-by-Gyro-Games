using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class TeleportationOnLastBulletOnHitTag : OnHitTag
{
    [SerializeField, InfoBox("Iterations to find position for teleportation")] private int iterations = 4;
    [SerializeField, BoxGroup("Bounds")] private int minAmmoBound = 5;
    [SerializeField, BoxGroup("Bounds")] private int maxAmmoBound = 100;
    [SerializeField, BoxGroup("Bounds")] private float minDistanceBound = 2f;
    [SerializeField, BoxGroup("Bounds")] private float maxDistanceBound = 5f;
    
    private OutFlowField outFlowField;
    private bool isLastBullet;
    
    public override void OnHit(OnHitTagData onHitTagData, ref CreepComponent creepComponent, ref float damage, Entity tower, EntityManager manager, EntityCommandBuffer ecb)
    {
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);

        if (attackerComponent.TowerType == AllEnums.TowerId.Rocket || attackerComponent.TowerType == AllEnums.TowerId.Mortar)
            isLastBullet = onHitTagData.AoeCollisionEvent.IsLastBulletProjectile;
        else
            isLastBullet = onHitTagData.GunCollisionEvent.ProjectileComponent.IsLastBullet;
        
        if (isLastBullet)
        {
            if(!manager.Exists(onHitTagData.CreepEntity))
                return;
            
            EntityQuery entityQuery = manager.CreateEntityQuery(new ComponentType[] {typeof(OutFlowField)});
            outFlowField = entityQuery.GetSingleton<OutFlowField>();
            
            float teleportDistance = Utilities.GetLerpedValue(minAmmoBound, maxAmmoBound, minDistanceBound, maxDistanceBound, attackerComponent.AttackStats.ReloadStats.MagazineSize);

            PositionComponent creepPositionComponent = manager.GetComponentData<PositionComponent>(onHitTagData.CreepEntity);
            
            TeleportationOnHitTag.SetUpTeleportation(iterations, outFlowField, creepPositionComponent, teleportDistance, onHitTagData.CreepEntity, manager);
            
            //TODO some visual for teleportation?
        }
    }
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/TeleportationOnLastBullet");
}