using ECSTest.Components;
using I2.Loc;
using Sirenix.Serialization;
using Unity.Entities;

public sealed class NoKnockbackTag : Tag, IStaticTag
{
    [OdinSerialize] public int OrderId { get; set; }
    
    public void ApplyStats(Tower tower) => tower.AttackStats.KnockBackPerBullet = 0;
    
    public void ApplyStats(Entity towerEntity, EntityManager manager)
    {
        var attacker = manager.GetComponentData<AttackerComponent>(towerEntity);
        attacker.AttackStats.KnockBackPerBullet = 0;
        manager.SetComponentData(towerEntity,attacker);
    }

    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/NoKnockback");
}