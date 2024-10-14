using ECSTest.Components;
using ECSTest.Systems;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class TimedBuffOnKill : OnKillTag
{
    [InfoBox("If you choose Penetration or Ricochet, remember that this directive shouldn't get on Rocket or Mortar")]
    [SerializeField, EnumToggleButtons] private AllEnums.BuffType buffType;
    [SerializeField, InfoBox("For Penetration and Ricochet use absolute numbers!!!")] private float bonusValue = .2f;
    [SerializeField] private float buffDuration;

    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        if(!handler.Exist())
            return;
        
        bool isGunStat = buffType is AllEnums.BuffType.Penetration or AllEnums.BuffType.Ricochet;
        int buffTypeValue = (int)buffType;
        DynamicBuffer<BuffBuffer> buffer = handler.Manager.GetBuffer<BuffBuffer>(handler.Tower);
        
        if (isGunStat)
        {
            GunStatsComponent gunStatsComponent = handler.Manager.GetComponentData<GunStatsComponent>(handler.Tower);
            BuffSystem.HandleBuff(buffType, bonusValue, ref gunStatsComponent, out float bonusStat);
            buffer.Add(new BuffBuffer() { BuffValue = bonusStat, Type = buffTypeValue, Timer = buffDuration });
            handler.Manager.SetComponentData(handler.Tower, gunStatsComponent);
        }
        else
        {
            AttackerComponent attackerComponent = handler.Manager.GetComponentData<AttackerComponent>(handler.Tower);
            BuffSystem.HandleBuff(buffType, bonusValue, ref attackerComponent, out float bonusStat);
            buffer.Add(new BuffBuffer() { BuffValue = bonusStat, Type = buffTypeValue, Timer = buffDuration });
            handler.Manager.SetComponentData(handler.Tower, attackerComponent);
        }
    }

    public override string GetDescription()
    {
        string statKey = buffType == AllEnums.BuffType.ReloadSpeed ? "TowerStats/ReloadSpeed" : $"TowerStats/{buffType}";
        string result = LocalizationManager.GetTranslation($"Tags/{nameof(TimedBuffOnKill)}")
                                            .Replace("{param1}", LocalizationManager.GetTranslation(statKey))
                                            .Replace("{param2}", GetValue())
                                            .Replace("{param3}", buffDuration.ToString());
        return result;

        string GetValue()
        {
            string result = "";
            if (buffType == AllEnums.BuffType.Penetration || buffType == AllEnums.BuffType.Ricochet)
            {
                result = bonusValue > 0 ? "+" + bonusValue.ToString() : bonusValue.ToString();
            }
            else
            {
                result = bonusValue > 0 ? "+" + Mathf.RoundToInt(bonusValue * 100) + "<color=#1fb2de>%</color>" : Mathf.RoundToInt(bonusValue * 100) + "<color=#1fb2de>%</color>";
            }

            return result;
        }
    }
}