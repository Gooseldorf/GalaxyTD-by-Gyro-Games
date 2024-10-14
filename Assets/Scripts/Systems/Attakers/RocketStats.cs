using ECSTest.Structs;
using I2.Loc;

public class RocketStats : AttackStats
{
    public float AOE;
    public float ScatterDistance;

    //public override AttackStatsStruct GetStats()
    //{
    //    var stat = base.GetStats();
    //    //TODO: add target count scater distance
    //    stat.AOE = AOE;
    //    return stat;
    //}

    public void SumAttackStats(ref RocketStatsComponent rocketStats)
    {
        rocketStats.AOE += AOE;
        rocketStats.ScatterDistance += ScatterDistance;
    }

    public void SubtractAttackStats(ref RocketStatsComponent rocketStats)
    {
        rocketStats.AOE -= AOE;
        rocketStats.ScatterDistance -= ScatterDistance;
    }

    public void MultiplyAttackStats(ref RocketStatsComponent rocketStats)
    {
        rocketStats.AOE += rocketStats.AOE * AOE;
        rocketStats.ScatterDistance += rocketStats.ScatterDistance * ScatterDistance;
    }

    public void DivideAttackStats(ref RocketStatsComponent rocketStats)
    {
        rocketStats.AOE -= rocketStats.AOE * AOE;
        rocketStats.ScatterDistance -= rocketStats.ScatterDistance * ScatterDistance;
    }

    public override void SumAttackStats(AttackStats add)
    {
        base.SumAttackStats(add);
        if (add is not RocketStats stats) return;

        AOE += stats.AOE;
        ScatterDistance += stats.ScatterDistance;
    }

    public override void SubtractAttackStats(AttackStats subtract)
    {
        base.SubtractAttackStats(subtract);

        if (subtract is not RocketStats stats) return;

        AOE -= stats.AOE;
        ScatterDistance -= stats.ScatterDistance;
    }

    public override void MultiplyAttackStats(AttackStats mult)
    {
        base.MultiplyAttackStats(mult);

        if (mult is not RocketStats stats) return;

        AOE += AOE * stats.AOE;
        ScatterDistance += ScatterDistance * stats.ScatterDistance;
    }

    public override void DivideAttackStats(AttackStats divide)
    {
        base.DivideAttackStats(divide);

        if (divide is not RocketStats stats) return;

        AOE -= AOE * stats.AOE;
        ScatterDistance -= ScatterDistance * stats.ScatterDistance;
    }

    public override string GetDescription(bool isPercent, bool showMagazineSizeMult = true, bool popUpUpgradeText = false)
    {
        string result = base.GetDescription(isPercent, showMagazineSizeMult, popUpUpgradeText);

        if (AOE != 0)
        {
            CheckLine(ref result);
            result += InsertValue(AOE,isPercent) + LocalizationManager.GetTranslation("TowerStats/AOE");
        }

        if (ScatterDistance != 0)
        {
            CheckLine(ref result);
            result += InsertValue(ScatterDistance, isPercent) + LocalizationManager.GetTranslation("TowerStats/ScatterDistance");
        }

        return result;
    }
}