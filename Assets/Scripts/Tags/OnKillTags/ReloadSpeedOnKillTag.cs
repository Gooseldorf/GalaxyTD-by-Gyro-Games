using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class ReloadSpeedOnKillTag : OnKillTag
{
    [SerializeField, InfoBox("Percent from ReloadSpeed. Positive number to increase reload speed, negative decrease")] private float increasePercent;

    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        AttackerComponent component = handler.Manager.GetComponentData<AttackerComponent>(handler.Tower);
        component.AttackStats.ReloadStats.ReloadTime /= (1 + increasePercent);
        handler.Manager.SetComponentData(handler.Tower, component);
    }
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/ReloadSpeedOnKill");
}