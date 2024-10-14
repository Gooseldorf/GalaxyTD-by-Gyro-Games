using I2.Loc;
using Sirenix.Serialization;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class OnlyTextTag : Tag, IStaticTag
{
    [OdinSerialize] public int OrderId { get; set; }

    public void ApplyStats(Tower tower) { }
    public void ApplyStats(Entity towerEntity, EntityManager manager)
    {
    }

    public override string GetDescription() => LocalizationManager.GetTranslation($"Tags/{this.name}");
}
