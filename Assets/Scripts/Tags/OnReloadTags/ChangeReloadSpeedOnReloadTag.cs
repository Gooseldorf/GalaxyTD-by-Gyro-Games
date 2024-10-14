using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class ChangeReloadSpeedOnReloadTag : OnReloadTag
{
    [SerializeField, InfoBox("Percent from ReloadSpeed. 100% is 1")] private float changePercent = 0.01f;

    public override void OnReload(Entity tower, EntityManager manager)
    {
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
        attackerComponent.AttackStats.ReloadStats.ReloadTime /= (1 + changePercent);

        manager.SetComponentData(tower, attackerComponent);
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/ChangeReloadSpeed")
                                                .Replace("{param}", (changePercent > 0 ? "+" + Mathf.RoundToInt(changePercent * 100) : Mathf.RoundToInt(changePercent * 100)) + "<color=#1fb2de>%</color>");
}