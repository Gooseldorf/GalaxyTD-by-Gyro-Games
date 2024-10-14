using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

public sealed class RestoresBulletOnKillTag : OnKillTag
{
    [SerializeField, InfoBox("Absolut number. Positive to restore")] private int bulletsCount;

    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        if(!handler.Exist())
            return;
        AttackerComponent component = handler.Manager.GetComponentData<AttackerComponent>(handler.Tower);
        component.Bullets = math.min(component.Bullets + bulletsCount, component.AttackStats.ReloadStats.MagazineSize);
        handler.Manager.SetComponentData(handler.Tower, component);
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/RestoresBulletOnKill")
                                                .Replace("{param}", bulletsCount.ToString());
}