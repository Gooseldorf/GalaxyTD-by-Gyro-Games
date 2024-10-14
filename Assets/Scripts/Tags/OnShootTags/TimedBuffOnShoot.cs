using ECSTest.Components;
using ECSTest.Systems;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public sealed class TimedBuffOnShoot : OnShootTag
{
    [InfoBox("If you choose Penetration or Ricochet, remember that this directive shouldn't get on Rocket or Mortar")]
    [SerializeField, EnumToggleButtons] private AllEnums.BuffType buffType;
    [SerializeField, InfoBox("For Penetration and Ricochet use absolute numbers!!!")] private float bonusValue = .2f;
    [SerializeField] private float buffDuration;
    
    public override void OnShoot(Entity tower, Entity shootEntity, EntityCommandBuffer ecb, EntityManager manager, DynamicBuffer<EntitiesBuffer> dynamicBuffer)
    {
        bool isGunStat = buffType is AllEnums.BuffType.Penetration or AllEnums.BuffType.Ricochet;
        int buffTypeValue = (int)buffType;
        DynamicBuffer<BuffBuffer> buffer = manager.GetBuffer<BuffBuffer>(tower);

        if (isGunStat)
        {
            GunStatsComponent gunStatsComponent = manager.GetComponentData<GunStatsComponent>(tower);
            BuffSystem.HandleBuff(buffType, bonusValue, ref gunStatsComponent, out float bonusStat);
            buffer.Add(new BuffBuffer() { BuffValue = bonusStat, Type = buffTypeValue, Timer = buffDuration });
            manager.SetComponentData(tower, gunStatsComponent);
        }
        else
        {
            AttackerComponent attackerComponent = manager.GetComponentData<AttackerComponent>(tower);
            BuffSystem.HandleBuff(buffType, bonusValue, ref attackerComponent, out float bonusStat);
            buffer.Add(new BuffBuffer() { BuffValue = bonusStat, Type = buffTypeValue, Timer = buffDuration });
            manager.SetComponentData(tower, attackerComponent);
        }
    }
    
    public override string GetDescription()
    {
        string statKey = buffType == AllEnums.BuffType.ReloadSpeed ? "TowerStats/ReloadSpeed" : $"TowerStats/{buffType}";
        string result = LocalizationManager.GetTranslation($"Tags/{nameof(TimedBuffOnShoot)}")
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
            else if (buffType == AllEnums.BuffType.ReloadSpeed)
            {
                
                result = -bonusValue > 0 ? "+" + (int)(-bonusValue * 100) + "<color=#1fb2de>%</color>" : (int)(-bonusValue * 100) + "<color=#1fb2de>%</color>";
            }
            else
            {
                result = bonusValue > 0 ? "+" + (int)(bonusValue * 100) + "<color=#1fb2de>%</color>" : (int)(bonusValue * 100) + "<color=#1fb2de>%</color>";
            }

            return result;
        }
    }
}