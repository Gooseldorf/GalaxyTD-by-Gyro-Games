using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class FearOnHitTag : OnHitTag
{
    [SerializeField, BoxGroup("Time Bounds")] private float minTimeBound = 1f;
    [SerializeField, BoxGroup("Time Bounds")] private float maxTimeBound = 5f;
    [SerializeField, BoxGroup("Time Bounds")] private float minPercentFromDmgBound = .1f;
    [SerializeField, BoxGroup("Time Bounds")] private float maxPercentFromDmgBound = .5f;
    
    [SerializeField, BoxGroup("Chance Bounds")] private float minProbabilityBound = .1f;
    [SerializeField, BoxGroup("Chance Bounds")] private float maxProbabilityBound = .9f;
    [SerializeField, BoxGroup("Chance Bounds")] private float minAmmoBound = 100f;
    [SerializeField, BoxGroup("Chance Bounds")] private float maxAmmoBound = 1f;
    
    public override void OnHit(OnHitTagData onHitTagData, ref CreepComponent creepComponent, ref float damage, Entity tower, EntityManager manager, EntityCommandBuffer ecb)
    {
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
        
        float percentFromHp = damage / creepComponent.MaxHp;
        float fearTime = Utilities.GetLerpedValue(minPercentFromDmgBound, maxPercentFromDmgBound, minTimeBound, maxTimeBound, percentFromHp);
        
        float fearChance = Utilities.GetLerpedValue(minAmmoBound, maxAmmoBound, minProbabilityBound, maxProbabilityBound, attackerComponent.AttackStats.ReloadStats.MagazineSize);
        float rand = Random.Range(0f, 100f);

        if (rand <= fearChance)
        {
            if(!manager.Exists(onHitTagData.CreepEntity))
                return;
            
            StunComponent stunComponent = manager.GetComponentData<StunComponent>(onHitTagData.CreepEntity);
            if (stunComponent.Time > 0)
            {
                stunComponent.Time = 0;
                manager.SetComponentData(onHitTagData.CreepEntity, stunComponent);
            }
            
            FearComponent fearComponent = manager.GetComponentData<FearComponent>(onHitTagData.CreepEntity);
            fearComponent.Time = fearTime;
            manager.SetComponentData(onHitTagData.CreepEntity, fearComponent);
        }
    }
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/FearOnHit");
}