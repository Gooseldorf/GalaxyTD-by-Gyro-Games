using ECSTest.Components;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine;

public sealed class AoeDamageOnKillTag : OnKillTag
{
    [SerializeField] private float range = 10f;
    [SerializeField, InfoBox("Percent from overkill damage. 100% is 1")] private float damagePercent = 1f;
    [SerializeField, InfoBox("Percent from attacker knockback. 100% is 1")] private float knockbackPercent = 1f;

    public override void OnKill(OnKillData handler, ref CreepComponent creepComponent)
    {
        if(!handler.Exist())
            return;
        
        AttackerComponent attackerComponent = handler.Manager.GetComponentData<AttackerComponent>(handler.Tower);
        float overkillDamage = -creepComponent.Hp;
        float aoeDamage = overkillDamage * damagePercent;
        float knockback = attackerComponent.AttackStats.KnockBackPerBullet * knockbackPercent;

        PositionComponent creepPositionComponent = handler.Manager.GetComponentData<PositionComponent>(handler.Creep);

        DamageSystem.DoTagAoeDamage(handler.CreepsLocator, creepPositionComponent.Position, range, aoeDamage, knockback, handler.Tower, handler.Manager, handler.CashComponent, handler.EntityCommandBuffer);
        //TODO need something like event for aoe visual
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/AoeDamageOnKill");
}