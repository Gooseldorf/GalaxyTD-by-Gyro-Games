using ECSTest.Components;
using Sirenix.Serialization;
using System;
using Unity.Entities;

public class StatsTag : Tag, IStaticTag
{
    [OdinSerialize] public int OrderId { get; set; }

    public bool IsPercent;

    [OdinSerialize, NonSerialized] private AttackStats attackStats = new AttackStats();

    public AttackStats AttackStats
    {
        get => attackStats;
        set => attackStats = value;
    }

    public void ApplyStats(Tower tower)
    {
        if (attackStats == null) return;

        if (IsPercent)
            tower.AttackStats.MultiplyAttackStats(attackStats);
        else
            tower.AttackStats.SumAttackStats(attackStats);
    }

    public void ApplyStats(Entity towerEntity, EntityManager manager)
    {
        var attacker = manager.GetComponentData<AttackerComponent>(towerEntity);
        if (IsPercent)
            attacker.AttackStats *= attackStats.GetStats();
        else
            attacker.AttackStats += attackStats.GetStats();
        manager.SetComponentData(towerEntity, attacker);
    }

    public override string GetDescription() => attackStats.GetDescription(IsPercent);
}