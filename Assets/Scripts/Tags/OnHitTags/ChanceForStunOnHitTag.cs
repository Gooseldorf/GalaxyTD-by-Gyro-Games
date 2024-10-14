using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class ChanceForStunOnHitTag : OnHitTag
{
    [SerializeField, BoxGroup("Time Bounds")] private float minTimeBound = 0.016f;
    [SerializeField, BoxGroup("Time Bounds")] private float maxTimeBound = 5f;
    [SerializeField, BoxGroup("Time Bounds")] private float minPercentFromDmgBound = 0f;
    [SerializeField, BoxGroup("Time Bounds")] private float maxPercentFromDmgBound = .5f;
    
    [SerializeField, BoxGroup("Chance Bounds")] private float minProbabilityBound = .1f;
    [SerializeField, BoxGroup("Chance Bounds")] private float maxProbabilityBound = .9f;
    [SerializeField, BoxGroup("Chance Bounds")] private float minAmmoBound = 100f;
    [SerializeField, BoxGroup("Chance Bounds")] private float maxAmmoBound = 1f;
    
    public override void OnHit(OnHitTagData onHitTagData, ref CreepComponent creepComponent, ref float damage, Entity tower, EntityManager manager, EntityCommandBuffer ecb)
    {
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
        
        float percentFromHp = damage / creepComponent.MaxHp;
        float stunDuration = Utilities.GetLerpedValue(minPercentFromDmgBound, maxPercentFromDmgBound, minTimeBound, maxTimeBound, percentFromHp);
        
        float stunProbability = Utilities.GetLerpedValue(minAmmoBound, maxAmmoBound, minProbabilityBound, maxProbabilityBound, attackerComponent.AttackStats.ReloadStats.MagazineSize);
        float rand = Random.Range(0f, 1f);

        if (rand <= stunProbability)
        {
            if(!manager.Exists(onHitTagData.CreepEntity))
                return;
            
            FearComponent fearComponent = manager.GetComponentData<FearComponent>(onHitTagData.CreepEntity);
            if(fearComponent.Time > 0) return;
            
            StunComponent stunComponent = manager.GetComponentData<StunComponent>(onHitTagData.CreepEntity);
            
            stunComponent.Time = stunDuration;
            
            manager.SetComponentData(onHitTagData.CreepEntity, stunComponent);
        }
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/ChanceForStun");
}