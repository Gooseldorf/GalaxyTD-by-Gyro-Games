using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class IncreaseCashBasedOnDamage : OnHitTag
{
    [SerializeField, InfoBox("Multiplier for damage percent from MaxHp")] private float damagePercentMultiplier = 2f;
    
    public override void OnHit(OnHitTagData onHitTagData, ref CreepComponent creepComponent, ref float damage, Entity tower, EntityManager manager, EntityCommandBuffer ecb)
    {
        float percentFromHp = math.min(damage, creepComponent.Hp) / creepComponent.MaxHp;
        float cashRewardChangeValue = percentFromHp * damagePercentMultiplier;

        creepComponent.CashRewardMultiplayer += cashRewardChangeValue;
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/IncreaseCash");
}