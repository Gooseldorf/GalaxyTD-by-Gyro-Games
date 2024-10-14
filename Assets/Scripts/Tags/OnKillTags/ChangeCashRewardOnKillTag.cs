using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class ChangeCashRewardOnKillTag : OnKillTag
{
    [SerializeField, EnumToggleButtons] private ChangeCashRewardType changeCashRewardToggle;
    [InfoBox("Absolute numbers. Positive for increase, negative decrease")]
    [SerializeField, ShowIf("changeCashRewardToggle", ChangeCashRewardType.Absolute)] private int changeAmount = 5;
    [InfoBox("Percent numbers. 100% is 1. Positive for increase, negative decrease")]
    [SerializeField, ShowIf("changeCashRewardToggle", ChangeCashRewardType.Percent)] private float changePercent = 1.0f;

    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        switch (changeCashRewardToggle)
        {
            case ChangeCashRewardType.NoCash:
                creepComponent.CashReward = 0;
                break;
            case ChangeCashRewardType.Absolute:
                creepComponent.CashReward += changeAmount;
                break;
            case ChangeCashRewardType.Percent:
                creepComponent.CashRewardMultiplayer *= changePercent;
                break;
        }
    }

    public override string GetDescription()
    {
        string result;

        switch (changeCashRewardToggle)
        {
            case ChangeCashRewardType.Absolute:
                result = LocalizationManager.GetTranslation("Tags/CashReward")
                            .Replace("{param}", changeAmount.ToString());
                break;
            case ChangeCashRewardType.Percent:
                result = LocalizationManager.GetTranslation("Tags/CashReward")
                        .Replace("{param}", (int)(changePercent * 100) + "<color=#1fb2de>%</color>");
                break;
            default:
                result = LocalizationManager.GetTranslation("Tags/CashRewardZero");
                break;
        }

        return result;
    }

    public enum ChangeCashRewardType { NoCash, Absolute, Percent }
}
