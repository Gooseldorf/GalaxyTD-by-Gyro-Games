using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class BankVaultOnKillTag : OnKillTag
{
    [SerializeField] private float increasePercent;

    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        if(!handler.Exist())
            return;
        CostComponent costComponent = handler.Manager.GetComponentData<CostComponent>(handler.Tower);

        costComponent.SellModifier += increasePercent;

        handler.Manager.SetComponentData(handler.Tower, costComponent);
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/BankVault")
                                                .Replace("{param}", ((increasePercent * 100)) + "<color=#1fb2de>%</color>");
}