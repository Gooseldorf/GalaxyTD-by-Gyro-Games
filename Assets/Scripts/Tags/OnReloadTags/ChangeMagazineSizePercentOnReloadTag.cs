using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class ChangeMagazineSizePercentOnReloadTag : OnReloadTag
{
    [SerializeField, InfoBox("Percent from MagazineSize. 100% is 1. Positive to increase MagazineSize, negative decrease")] private float changePercent = 0.05f;

    public override void OnReload(Entity tower, EntityManager manager)
    {
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
        int changeAmount = (int)math.max(1, attackerComponent.AttackStats.ReloadStats.MagazineSize * changePercent);
        attackerComponent.AttackStats.ReloadStats.RawMagazineSize = math.max(attackerComponent.AttackStats.ReloadStats.RawMagazineSize + changeAmount, 1);
        
        manager.SetComponentData(tower, attackerComponent);
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/ChangeMagazineSizePercent")
                                                .Replace("{param}", changePercent > 0 ? "+" : "" + Mathf.RoundToInt(changePercent * 100) + "<color=#1fb2de>%</color>");
}