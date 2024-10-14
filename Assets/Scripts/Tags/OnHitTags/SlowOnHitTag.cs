using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class SlowOnHitTag : OnHitTag
{
    [SerializeField, BoxGroup("Bounds")] private float minSlowAmountBound = 0.1f;
    [SerializeField, BoxGroup("Bounds")] private float maxSlowAmountBound = 0.5f;
    [SerializeField, BoxGroup("Bounds")] private float minPercentFromDmgBound = .1f;
    [SerializeField, BoxGroup("Bounds")] private float maxPercentFromDmgBound = .5f;
    [SerializeField] private float slowTime = 1f;

    public override void OnHit(OnHitTagData onHitTagData, ref CreepComponent creepComponent, ref float damage, Entity tower, EntityManager manager, EntityCommandBuffer ecb)
    {
        if(!manager.Exists(onHitTagData.CreepEntity))
            return;
        
        float damagePercent = damage / creepComponent.MaxHp;
        float slowAmount = Utilities.GetLerpedValue(minPercentFromDmgBound, maxPercentFromDmgBound, minSlowAmountBound, maxSlowAmountBound, damagePercent);
        
        SlowComponent slowComponent = manager.GetComponentData<SlowComponent>(onHitTagData.CreepEntity);
        slowComponent.Percent = slowAmount;
        slowComponent.Time = slowTime;
        
        manager.SetComponentData(onHitTagData.CreepEntity, slowComponent);
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/Slow");
}