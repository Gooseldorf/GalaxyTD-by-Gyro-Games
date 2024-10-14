using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class RadiationOnKillTag : OnKillTag
{
    [SerializeField, InfoBox("Percent from overkill damage. From 100% to 300%. 100% is 1 "), Range(1, 3)] private float dotDamagePercent = 1;
    [SerializeField] private float dotTime = 5;
    [SerializeField] private float range = 10f;
    
    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        float overkillDamage = -creepComponent.Hp;
        float dotDamage = overkillDamage * dotDamagePercent;
        
        handler.AoeEffectOnKill(range, (creepInfo) =>
        {
            Entity creepEntity = creepInfo.Entity;
            RadiationComponent creepInRange = handler.Manager.GetComponentData<RadiationComponent>(creepEntity);
            creepInRange.DPS = dotDamage;
            creepInRange.Time = dotTime;
            handler.Manager.SetComponentData(creepEntity, creepInRange);
        });
        
        //TODO need something like event for aoe visual
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/RadiationOnKill")
                                                .Replace("{param}", range.ToString());
}