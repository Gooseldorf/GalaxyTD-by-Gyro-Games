using I2.Loc;
using Sirenix.Serialization;
using Systems.Attakers;
using UnityEngine;

public sealed class FasterBulletsForAoeTag : Tag, IStaticTag
{
    [SerializeField] private float increasePercent = 0.5f;

    [OdinSerialize] public int OrderId { get; set; }

    public void ApplyStats(Tower tower)
    {
        switch (tower.AttackStats)
        {
            case RocketStats stats:
                stats.ProjectileSpeed += stats.ProjectileSpeed * increasePercent;
                break;
            case MortarStats mortarStats:
                mortarStats.ArrivalTime += mortarStats.ArrivalTime * increasePercent;
                break;
        }
    }

    public override string GetDescription() => increasePercent switch
    {
        < 1 => LocalizationManager.GetTranslation("Tags/FasterBulletsForAoe"),
        _ => LocalizationManager.GetTranslation("Tags/FasterBulletsForAoe1")
    };
}