using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class MagazineOnKillTag : OnKillTag
{
    [SerializeField, InfoBox("absolute numbers. Positive increase, negative decrease")] private int increaseCount;

    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        if(!handler.Exist())
            return;
        AttackerComponent component = handler.Manager.GetComponentData<AttackerComponent>(handler.Tower);
        component.AttackStats.ReloadStats.RawMagazineSize += increaseCount;
        handler.Manager.SetComponentData(handler.Tower, component);
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/MagazineOnKill")
                                                .Replace("{param}", increaseCount.ToString());
}