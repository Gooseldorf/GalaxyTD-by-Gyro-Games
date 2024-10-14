using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class SlowInAreaOnKillTag : OnKillTag
{
    [SerializeField, BoxGroup("Bounds")] private float minSlowAmount = 0.2f;
    [SerializeField, BoxGroup("Bounds")] private float maxSlowAmount = 0.6f;
    [SerializeField, BoxGroup("Bounds")] private float minPercentFromHpBound = 0.0f;
    [SerializeField, BoxGroup("Bounds")] private float maxPercentFromHpBound = 2f;
    [SerializeField] private float slowTime = 5f;
    [SerializeField] private float range = 10f;
    
    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        float overkillDamage = -creepComponent.Hp;
        float percentFromHp = overkillDamage / creepComponent.MaxHp;
        float slowAmount = Utilities.GetLerpedValue(minPercentFromHpBound, maxPercentFromHpBound, minSlowAmount, maxSlowAmount, percentFromHp);
        
        handler.AoeEffectOnKill(range, (creepInfo) =>
        {
            Entity creepEntity = creepInfo.Entity;
            SlowComponent creepInRange = handler.Manager.GetComponentData<SlowComponent>(creepEntity);
            creepInRange.Percent = slowAmount;
            creepInRange.Time = slowTime;
            handler.Manager.SetComponentData(creepEntity, creepInRange);
        });
        //TODO need something like event for aoe visual
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/SlowInAreaOnKill");
}