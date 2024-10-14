using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class RangeOnKillTag : OnKillTag
{
    [SerializeField, InfoBox("Percent from Range. 100% is 1. Positive fro increase, negative decrease")] private float increasePercent = 0.01f;

    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        if(!handler.Exist())
            return;
        AttackerComponent component = handler.Manager.GetComponentData<AttackerComponent>(handler.Tower);
        component.AttackStats.AimingStats.Range += component.AttackStats.AimingStats.Range * increasePercent;
        handler.Manager.SetComponentData(handler.Tower, component);
    }
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/RangeOnKill");
}