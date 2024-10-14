using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class ChangeMassOnHitTag : OnHitTag
{
    [SerializeField, InfoBox("Negative numbers for decrease, positive for increase")] private float damageToMaxHpModifier = -0.75f;
    [SerializeField] private float damageToMaxHpTreshhold = 0.05f;

    public override void OnHit(OnHitTagData onHitTagData, ref CreepComponent creepComponent, ref float damage, Entity tower, EntityManager manager, EntityCommandBuffer ecb)
    {
        if (damage < damageToMaxHpTreshhold * creepComponent.MaxHp)
            return;

        float massChangeValue = damageToMaxHpModifier * damage / creepComponent.MaxHp;
        creepComponent.Mass = math.max(creepComponent.Mass + massChangeValue, 1);
    }

    public override string GetDescription() => LocalizationManager.GetTranslation(damageToMaxHpModifier > 0 ? "Tags/IncreaseMass" : "Tags/DecreaseMass");
}