using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class StunOnLastBulletOnHitTag : OnHitTag
{
    [SerializeField, BoxGroup("Bounds")] private int minAmmoBound = 5;
    [SerializeField, BoxGroup("Bounds")] private int maxAmmoBound = 50;
    [SerializeField, BoxGroup("Bounds")] private float minTimeBound = .2f;
    [SerializeField, BoxGroup("Bounds")] private float maxTimeBound = 5f;

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
            
            FearComponent fearComponent = manager.GetComponentData<FearComponent>(onHitTagData.CreepEntity);
            if(fearComponent.Time > 0) return;
            
            float stunDuration = Utilities.GetLerpedValue(minAmmoBound, maxAmmoBound, minTimeBound, maxTimeBound, attackerComponent.AttackStats.ReloadStats.MagazineSize);
            
            StunComponent stunComponent = manager.GetComponentData<StunComponent>(onHitTagData.CreepEntity);
            stunComponent.Time = stunDuration;
            
            manager.SetComponentData(onHitTagData.CreepEntity, stunComponent);
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/StunOnLastBullet");
}