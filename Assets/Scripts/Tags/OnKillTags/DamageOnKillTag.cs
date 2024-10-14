using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class DamageOnKillTag : OnKillTag
{
    [SerializeField, InfoBox("Percent from DamagePerBullet. 100% is 1")] private float increasePercent;

    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        if(!handler.Exist())
            return;
        AttackerComponent component = handler.Manager.GetComponentData<AttackerComponent>(handler.Tower);
        component.AttackStats.DamagePerBullet += component.AttackStats.DamagePerBullet * increasePercent;
        handler.Manager.SetComponentData(handler.Tower, component);
    }
    
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/DamageOnKill");
}