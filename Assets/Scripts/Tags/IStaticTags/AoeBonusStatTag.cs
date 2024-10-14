using I2.Loc;
using Sirenix.Serialization;
using Systems.Attakers;
using UnityEngine;

public sealed class AoeBonusStatTag : Tag, IStaticTag
{
    [SerializeField] private float aoeBonusPercent;

    [OdinSerialize] public int OrderId { get; set; }

    public void ApplyStats(Tower tower)
    {
        if (tower.AttackStats is RocketStats rocketStats)
            rocketStats.AOE += rocketStats.AOE * aoeBonusPercent;
        else if (tower.AttackStats is MortarStats mortarStats)
            mortarStats.AOE += mortarStats.AOE * aoeBonusPercent;
    }

    public override string GetDescription() => (aoeBonusPercent > 0 ? "+" : "") + (int)(aoeBonusPercent * 100) + "<color=#1fb2de>%</color> " + LocalizationManager.GetTranslation("TowerStats/AOE");
}