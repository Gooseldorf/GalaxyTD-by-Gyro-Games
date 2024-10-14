using ECSTest.Components;
using I2.Loc;
using Sirenix.Serialization;
using Unity.Entities;

public sealed class NoShotCostTag : Tag, IStaticTag
{
    [OdinSerialize] public int OrderId { get; set; }

    public void ApplyStats(Tower tower) => tower.AttackStats.ReloadStats.BulletCost = 0;

    public void ApplyStats(Entity towerEntity, EntityManager manager)
    {
        var attackerComponent = manager.GetComponentData<AttackerComponent>(towerEntity);
        attackerComponent.AttackStats.ReloadStats.BulletCost = 0;
        manager.SetComponentData(towerEntity, attackerComponent);
    }


    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/NoShotCost");
}