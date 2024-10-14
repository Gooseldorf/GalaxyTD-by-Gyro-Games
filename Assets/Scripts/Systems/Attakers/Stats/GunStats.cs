using ECSTest.Structs;
using I2.Loc;
using System;
using UnityEngine;

[Serializable]
public class GunStats : AttackStats
{
    public RicochetStats RicochetStats = new();
    public AccuracyStats AccuracyStats = new();
    public float DamageFallof;


    //public override AttackStatsStruct GetStats()
    //{
    //    AttackStatsStruct attackStats = base.GetStats();
    //    attackStats.DamageFallof = DamageFallof;
    //    attackStats.RicochetCount = RicochetStats.RicochetCount;
    //    attackStats.PenetrationCount = RicochetStats.PenetrationCount;
    //    attackStats.DamageChangePerPenetration = RicochetStats.DamageChangePerPenetration;
    //    attackStats.DamageChangePerRicochet = RicochetStats.DamageChangePerRicochet;
    //    return attackStats;
    //}

    public void SumAttackStats(ref GunStatsComponent gunStats)
    {
        gunStats.RicochetCount += RicochetStats.RicochetCount;
        gunStats.PenetrationCount += RicochetStats.PenetrationCount;
        gunStats.RicochetDeviation += RicochetStats.RicochetDeviation;
        gunStats.DamageMultPerPenetration += RicochetStats.DamageMultPerPenetration;
        gunStats.DamageMultPerRicochet += RicochetStats.DamageMultPerRicochet;

        gunStats.Deviation += AccuracyStats.Deviation;
        gunStats.Control += AccuracyStats.Control;
        gunStats.Recoil += AccuracyStats.Recoil;

        gunStats.DamageFallof += DamageFallof;
    }

    public void SubtractAttackStats(ref GunStatsComponent gunStats)
    {
        gunStats.RicochetCount -= RicochetStats.RicochetCount;
        gunStats.PenetrationCount -= RicochetStats.PenetrationCount;
        gunStats.RicochetDeviation -= RicochetStats.RicochetDeviation;
        gunStats.DamageMultPerPenetration -= RicochetStats.DamageMultPerPenetration;
        gunStats.DamageMultPerRicochet -= RicochetStats.DamageMultPerRicochet;

        gunStats.Deviation -= AccuracyStats.Deviation;
        gunStats.Control -= AccuracyStats.Control;
        gunStats.Recoil -= AccuracyStats.Recoil;

        gunStats.DamageFallof -= DamageFallof;
    }

    public void MultiplyAttackStats(ref GunStatsComponent gunStats)
    {
        gunStats.RicochetCount += RicochetStats.RicochetCount;
        gunStats.PenetrationCount += RicochetStats.PenetrationCount;
        gunStats.RicochetDeviation += RicochetStats.RicochetDeviation * gunStats.RicochetDeviation;
        gunStats.DamageMultPerPenetration += RicochetStats.DamageMultPerPenetration;
        gunStats.DamageMultPerRicochet += RicochetStats.DamageMultPerRicochet;

        gunStats.Deviation += AccuracyStats.Deviation * gunStats.Deviation;
        gunStats.Control += AccuracyStats.Control * gunStats.Control;
        gunStats.Recoil += AccuracyStats.Recoil * gunStats.Recoil;

        gunStats.DamageFallof += gunStats.DamageFallof * DamageFallof;
    }

    public void DivideAttackStats(ref GunStatsComponent gunStats)
    {
        gunStats.RicochetCount -= RicochetStats.RicochetCount;
        gunStats.PenetrationCount -= RicochetStats.PenetrationCount;
        gunStats.RicochetDeviation -= RicochetStats.RicochetDeviation * gunStats.RicochetDeviation;
        gunStats.DamageMultPerPenetration -= RicochetStats.DamageMultPerPenetration;
        gunStats.DamageMultPerRicochet -= RicochetStats.DamageMultPerRicochet;

        gunStats.Deviation -= AccuracyStats.Deviation * gunStats.Deviation;
        gunStats.Control -= AccuracyStats.Control * gunStats.Control;
        gunStats.Recoil -= AccuracyStats.Recoil * gunStats.Recoil;

        gunStats.DamageFallof -= gunStats.DamageFallof * DamageFallof;
    }

    public override object Clone()
    {
        var clone = (GunStats)base.Clone();
        clone.RicochetStats = RicochetStats?.Clone() as RicochetStats;
        clone.AccuracyStats = AccuracyStats?.Clone() as AccuracyStats;
        return clone;
    }

    public override void SumAttackStats(AttackStats add)
    {
        base.SumAttackStats(add);

        if (add is not GunStats stats) return;

        RicochetStats += stats.RicochetStats;
        AccuracyStats += stats.AccuracyStats;
        DamageFallof += stats.DamageFallof;
    }

    public override void SubtractAttackStats(AttackStats subtract)
    {
        base.SubtractAttackStats(subtract);

        if (subtract is not GunStats stats) return;

        RicochetStats -= stats.RicochetStats;
        AccuracyStats -= stats.AccuracyStats;
        DamageFallof -= stats.DamageFallof;
    }

    public override void MultiplyAttackStats(AttackStats mult)
    {
        base.MultiplyAttackStats(mult);

        if (mult is not GunStats stats) return;

        RicochetStats *= stats.RicochetStats;
        AccuracyStats *= stats.AccuracyStats;
        DamageFallof += DamageFallof * stats.DamageFallof;
    }

    public override void DivideAttackStats(AttackStats divide)
    {
        base.DivideAttackStats(divide);

        if (divide is not GunStats stats) return;

        RicochetStats /= stats.RicochetStats;
        AccuracyStats /= stats.AccuracyStats;
        DamageFallof -= DamageFallof * stats.DamageFallof;
    }

    public override string GetDescription(bool isPercent, bool showMagazineSizeMult = true, bool popUpUpgradeText = false)
    {
        string result = base.GetDescription(isPercent, showMagazineSizeMult, popUpUpgradeText);

        // Ricochet Stats
        if (RicochetStats.RicochetCount != 0)
        {
            CheckLine(ref result);
            result += InsertValue(RicochetStats.RicochetCount, false) + LocalizationManager.GetTranslation("Tags/RicochetCount");
        }
        if (RicochetStats.PenetrationCount != 0)
        {
            CheckLine(ref result);
            result += InsertValue(RicochetStats.PenetrationCount, false) + LocalizationManager.GetTranslation("Tags/PenetrationCount");
        }
        if (RicochetStats.RicochetDeviation != 0)
        {
            CheckLine(ref result);
            result += InsertValue(RicochetStats.RicochetDeviation, isPercent) + LocalizationManager.GetTranslation("TowerStats/RicochetDeviation");
        }
        if (RicochetStats.DamageMultPerPenetration != 0)
        {
            CheckLine(ref result);
            result += InsertValue(RicochetStats.DamageMultPerPenetration, true) + LocalizationManager.GetTranslation("TowerStats/DamageChangePerPenetration");
        }
        if (RicochetStats.DamageMultPerRicochet != 0)
        {
            CheckLine(ref result);
            result += InsertValue(RicochetStats.DamageMultPerRicochet, true) + LocalizationManager.GetTranslation("TowerStats/DamageChangePerRicochet");
        }
        // Accuracy Stats
        if (AccuracyStats.Deviation != 0)
        {
            CheckLine(ref result);
            result += InsertValue(AccuracyStats.Deviation, isPercent) + LocalizationManager.GetTranslation("TowerStats/Deviation");
        }
        if (AccuracyStats.Control != 0)
        {
            CheckLine(ref result);
            result += InsertValue(AccuracyStats.Control, isPercent) + LocalizationManager.GetTranslation("TowerStats/Control");
        }
        if (AccuracyStats.Recoil != 0)
        {
            CheckLine(ref result);
            result += InsertValue(AccuracyStats.Recoil, isPercent) + LocalizationManager.GetTranslation("TowerStats/Recoil");
        }
        return result;
    }
}