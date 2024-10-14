using ECSTest.Components;
using I2.Loc;
using Sirenix.Serialization;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

public sealed class OnlyManualReloadTag : Tag, IStaticTag
{
    [OdinSerialize]
    public int OrderId { get; set; }

    public void ApplyStats(Tower tower) => tower.AutoReload = false;
    public void ApplyStats(Entity towerEntity, EntityManager manager)
    {
        var attackerComponent = manager.GetComponentData<AttackerComponent>(towerEntity);
        attackerComponent.AutoReload = false;
        manager.SetComponentData(towerEntity,attackerComponent);
    }


    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/OnlyManualReload");
}
