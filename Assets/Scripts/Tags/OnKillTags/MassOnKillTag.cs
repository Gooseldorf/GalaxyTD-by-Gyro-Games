using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class MassOnKillTag : OnKillTag
{
    [SerializeField, InfoBox("Percent from mass. 100% is 1. Positive increase, negative decrease")] private float massChangePercent = 0.05f;
    [SerializeField] private float range = 10f;

    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        handler.AoeEffectOnKill(range, (creepInfo) =>
        {
            Entity creepEntity = creepInfo.Entity;
            CreepComponent creepInRange = handler.Manager.GetComponentData<CreepComponent>(creepEntity);
            creepInRange.Mass += creepInRange.Mass * massChangePercent;
            handler.Manager.SetComponentData(creepEntity, creepInRange);
        });

        //TODO need something like event for aoe visual
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/MassOnKill");
}