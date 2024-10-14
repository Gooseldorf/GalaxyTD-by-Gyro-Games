using CardTD.Utilities;
using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class FearInAreaOnKillTag : OnKillTag
{
    [SerializeField, BoxGroup("Bounds")] private float minTimeBound = 0.2f;
    [SerializeField, BoxGroup("Bounds")] private float maxTimeBound = 3f;
    [SerializeField, BoxGroup("Bounds")] private float minPercentFromHpBound = 0.5f;
    [SerializeField, BoxGroup("Bounds")] private float maxPercentFromHpBound = 1.5f;
    [SerializeField] private float range = 1.5f;

    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        float overkillDamage = -creepComponent.Hp;
        float percentFromHp = overkillDamage / creepComponent.MaxHp;
        float fearTime = Utilities.GetLerpedValue(minPercentFromHpBound, maxPercentFromHpBound, minTimeBound, maxTimeBound, percentFromHp);
        
        handler.AoeEffectOnKill(range, (creepInfo) =>
        {
            Entity creepEntity = creepInfo.Entity;
            
            StunComponent stunComponent = handler.Manager.GetComponentData<StunComponent>(creepEntity);
            if (stunComponent.Time > 0)
            {
                stunComponent.Time = 0;
                handler.Manager.SetComponentData(creepEntity, stunComponent);
            }
            
            FearComponent fearComponent = handler.Manager.GetComponentData<FearComponent>(creepEntity);
            fearComponent.Time = fearTime;
            handler.Manager.SetComponentData(creepEntity, fearComponent);
        });

        //TODO need something like event for aoe visual
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/FearInArea")
                                                .Replace("{param}", range.ToString());
}