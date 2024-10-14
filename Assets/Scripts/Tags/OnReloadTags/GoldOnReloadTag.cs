using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class GoldOnReloadTag : OnReloadTag
{
    [SerializeField, InfoBox("Percent from ReloadCost back. 100% is 1. Positive numbers to add cash")] private float percent = 0.5f;

    public override void OnReload(Entity tower, EntityManager manager) 
    {
        AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
        CashComponent cashComponent = GameServices.Instance.GetCashComponent(manager);
        
        float cashChangeValue = attackerComponent.AttackStats.ReloadStats.ReloadCost * percent;
        cashComponent.AddCash((int)math.round(cashChangeValue)); 
        
        manager.SetComponentData(tower, attackerComponent);
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/GoldOnReload")
                                                .Replace("{param}", (percent*100).ToString() + "<color=#1fb2de>%</color>");
}