using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class StunInAreaOnKillTag : OnKillTag
{
    [SerializeField, BoxGroup("Bounds")] private float minTimeBound = 0.016f;
    [SerializeField, BoxGroup("Bounds")] private float maxTimeBound = 3f;
    [SerializeField, BoxGroup("Bounds")] private float minPercentFromHpBound = 0f;
    [SerializeField, BoxGroup("Bounds")] private float maxPercentFromHpBound = 2f;
    [SerializeField] private float range = 10f;
    
    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        float overkillDamage = -creepComponent.Hp;
        float percentFromHp = overkillDamage / creepComponent.MaxHp;
        float stunTime = Utilities.GetLerpedValue(minPercentFromHpBound, maxPercentFromHpBound, minTimeBound, maxTimeBound, percentFromHp);
        
        handler.AoeEffectOnKill(range, (creepInfo) =>
        {
            Entity creepEntity = creepInfo.Entity;
            FearComponent fearComponent = handler.Manager.GetComponentData<FearComponent>(creepEntity);
            if(fearComponent.Time > 0) return;
            
            StunComponent stunComponent = handler.Manager.GetComponentData<StunComponent>(creepEntity);
            stunComponent.Time = stunTime;
            handler.Manager.SetComponentData(creepEntity, stunComponent);
        });
        
        //TODO need something like event for aoe visual
    }
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/StunInAreaOnKill");
}