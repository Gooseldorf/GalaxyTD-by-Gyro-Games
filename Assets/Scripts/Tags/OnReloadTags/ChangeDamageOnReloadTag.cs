using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class ChangeDamageOnReloadTag : OnReloadTag
{
    [SerializeField, InfoBox("Percent from DamagePerBullet. 100% is 1. Positive numbers for increase DamagePerBullet, negative decrease")]
    private float changePercent = 0.01f;

    public override void OnReload(Entity tower, EntityManager manager)
    {
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
        attackerComponent.AttackStats.DamagePerBullet += attackerComponent.AttackStats.DamagePerBullet * changePercent;
            
        manager.SetComponentData(tower, attackerComponent);
    }

    public override string GetDescription() => changePercent switch
    {
        < 0 => LocalizationManager.GetTranslation("Tags/DecreaseDamageOnReload"),
        _ => LocalizationManager.GetTranslation("Tags/IncreaseDamageOnReload")
    };
}