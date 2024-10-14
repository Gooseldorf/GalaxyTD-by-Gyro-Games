using ECSTest.Structs;
using I2.Loc;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

[Serializable]
public class AttackStats : ICloneable
{
    //TODO: damage modifiers and add  component to tower
    public float ProjectileSpeed;
    public float DamagePerBullet;
    public float KnockBackPerBullet;
    public float Sellmodifier;

    public ReloadStats ReloadStats = new();
    public AimingStats AimingStats = new();
    public ShootingStats ShootingStats = new();

    [ShowInInspector] public float FirerateChangePercent => ((1 / (ShootingStats.ShotDelay + 1)) - 1) * 100;
    [ShowInInspector] public float ReloadSpeedChangePercent => ((1 / (ReloadStats.ReloadTime + 1)) - 1) * 100;

    //public float Firerate => ShootingStats.ShotDelay != 0 ? 1 / ShootingStats.ShotDelay : 0;
    public float DPS => (DamagePerBullet * ReloadStats.MagazineSize) /
                        ((ReloadStats.MagazineSize - 1) * ShootingStats.ShotDelay + ReloadStats.ReloadTime);

    public virtual object Clone()
    {
        AttackStats clone = (AttackStats)MemberwiseClone();

        clone.ReloadStats = ReloadStats?.Clone() as ReloadStats;
        clone.AimingStats = AimingStats?.Clone() as AimingStats;
        clone.ShootingStats = ShootingStats?.Clone() as ShootingStats;

        return clone;
    }

    public AttackStatsStruct GetStats()
    {
        return new AttackStatsStruct
        {
            ProjectileSpeed = ProjectileSpeed,
            DamagePerBullet = DamagePerBullet,
            KnockBackPerBullet = KnockBackPerBullet,
            ReloadStats = new ReloadStatsStruct()
            {
                BulletCost = ReloadStats.BulletCost,
                RawMagazineSize = ReloadStats.RawMagazineSize,
                ReloadTime = ReloadStats.ReloadTime
            },
            AimingStats = new AimingStatsStruct()
            {
                Range = AimingStats.Range,
                AttackAngle = AimingStats.AttackAngle,
                RotationSpeed = AimingStats.RotationSpeed
            },
            ShootingStats = new ShootingStatsStruct
            {
                ShotDelay = ShootingStats.ShotDelay,
                AvailableAttackPatterns = ShootingStats.AvailableAttackPatterns,
                ShotsPerBurst = ShootingStats.ShotsPerBurst,
                ProjectilesPerShot = ShootingStats.ProjectilesPerShot,
                WindUpTime = ShootingStats.WindUpTime
            },
        };
    }

    public virtual void SumAttackStats(AttackStats add)
    {
        ProjectileSpeed += add.ProjectileSpeed;
        DamagePerBullet += add.DamagePerBullet;
        KnockBackPerBullet += add.KnockBackPerBullet;
        Sellmodifier += add.Sellmodifier;

        ReloadStats += add.ReloadStats;
        AimingStats += add.AimingStats;
        ShootingStats += add.ShootingStats;
    }

    public virtual void SubtractAttackStats(AttackStats subtract)
    {
        ProjectileSpeed -= subtract.ProjectileSpeed;
        DamagePerBullet -= subtract.DamagePerBullet;
        KnockBackPerBullet -= subtract.KnockBackPerBullet;
        Sellmodifier -= subtract.Sellmodifier;

        ReloadStats -= subtract.ReloadStats;
        AimingStats -= subtract.AimingStats;
        ShootingStats -= subtract.ShootingStats;
    }

    public virtual void MultiplyAttackStats(AttackStats mult)
    {
        ProjectileSpeed += ProjectileSpeed * mult.ProjectileSpeed;
        DamagePerBullet += DamagePerBullet * mult.DamagePerBullet;
        KnockBackPerBullet += KnockBackPerBullet * mult.KnockBackPerBullet;
        Sellmodifier += Sellmodifier * mult.Sellmodifier;

        ReloadStats *= mult.ReloadStats;
        AimingStats *= mult.AimingStats;
        ShootingStats *= mult.ShootingStats;
    }

    public virtual void DivideAttackStats(AttackStats divide)
    {
        ProjectileSpeed -= ProjectileSpeed * divide.ProjectileSpeed;
        DamagePerBullet -= DamagePerBullet * divide.DamagePerBullet;
        KnockBackPerBullet -= KnockBackPerBullet * divide.KnockBackPerBullet;
        Sellmodifier -= Sellmodifier * divide.Sellmodifier;

        ReloadStats /= divide.ReloadStats;
        AimingStats /= divide.AimingStats;
        ShootingStats /= divide.ShootingStats;
    }

    public virtual string GetDescription(bool isPercent, bool showMagazineSizeMult = true, bool popUpUpgradeText = false)
    {
        string result = "";
        // Attack Stats
        if (ProjectileSpeed != 0)
            result = InsertValue(ProjectileSpeed, isPercent) + LocalizationManager.GetTranslation("TowerStats/ProjectileSpeed");
        if (DamagePerBullet != 0)
        {
            CheckLine(ref result);
            result += InsertValue(DamagePerBullet, isPercent) + LocalizationManager.GetTranslation("TowerStats/Damage");
        }
        if (KnockBackPerBullet != 0 && !popUpUpgradeText)
        {
            CheckLine(ref result);
            result += InsertValue(KnockBackPerBullet, isPercent) + LocalizationManager.GetTranslation("TowerStats/KnockBack");
        }
        // Reload Stats
        if (ReloadStats.RawMagazineSize != 0)
        {
            if (showMagazineSizeMult)
            {
                CheckLine(ref result);
                result += InsertValue(ReloadStats.RawMagazineSize, isPercent) + LocalizationManager.GetTranslation("TowerStats/MagazineSize");
            }
            else if(ReloadStats.RawMagazineSize > 1)
            {
                CheckLine(ref result);
                result += InsertValue(ReloadStats.RawMagazineSize - 1, isPercent) + LocalizationManager.GetTranslation("TowerStats/MagazineSize");
            }

        }
        if (ReloadStats.BulletCost != 0)
        {
            CheckLine(ref result);
            result += InsertValue(ReloadStats.BulletCost, isPercent) + LocalizationManager.GetTranslation("TowerStats/AmmoCost");
        }
        if (ReloadStats.ReloadTime != 0)
        {
            CheckLine(ref result);
            if(isPercent)
                result += InsertValue(((1 / (ReloadStats.ReloadTime + 1)) - 1), true) + LocalizationManager.GetTranslation("TowerStats/ReloadSpeed");
            else
                result += InsertValue(-ReloadStats.ReloadTime, false) + LocalizationManager.GetTranslation("TowerStats/ReloadSpeed");
        }
        // Aiming Stats
        if (AimingStats.Range != 0)
        {
            CheckLine(ref result);
            result += InsertValue(AimingStats.Range, isPercent) + LocalizationManager.GetTranslation("TowerStats/Range");
        }
        if (AimingStats.RotationSpeed != 0)
        {
            CheckLine(ref result);
            result += InsertValue(AimingStats.RotationSpeed, isPercent) + LocalizationManager.GetTranslation("TowerStats/RotationSpeed");
        }
        if (AimingStats.AttackAngle != 0)
        {
            CheckLine(ref result);
            result += InsertValue(AimingStats.AttackAngle, isPercent) + LocalizationManager.GetTranslation("TowerStats/AttackAngle");
        }
        if (ShootingStats.ShotsPerBurst != 0)
        {
            CheckLine(ref result);
            result += InsertValue(ShootingStats.ShotsPerBurst, false) + LocalizationManager.GetTranslation("TowerStats/ShotsPerBurst");
        }
        if (ShootingStats.ShotDelay != 0)
        {
            CheckLine(ref result);
                result += InsertValue(FirerateChangePercent/100, isPercent) + LocalizationManager.GetTranslation("TowerStats/Firerate");
        }
        if (ShootingStats.ProjectilesPerShot != 0)
        {
            CheckLine(ref result);
            result += InsertValue(ShootingStats.ProjectilesPerShot, false) + LocalizationManager.GetTranslation("TowerStats/ProjectilesPerShot");
        }
        if (ShootingStats.WindUpTime != 0)
        {
            CheckLine(ref result);
            result += InsertValue(ShootingStats.WindUpTime, isPercent) + LocalizationManager.GetTranslation("TowerStats/WindUpTime");
        }
        if (Sellmodifier != 0)
        {
            CheckLine(ref result);
            result += InsertValue(Sellmodifier, isPercent) + LocalizationManager.GetTranslation("TowerStats/SellModifier");
        }

        return result;
    }

    protected void CheckLine(ref string s)
    {
        if (s != "") s += "\n<color=#1fb2de>></color> ";
    }

    protected string InsertSpace(bool isPercent) => isPercent ? "% " : " ";

    protected string InsertValue(float value, bool isPercent)
    {
        return (value > 0 ? "+" : "") + (isPercent ? Mathf.RoundToInt(value * 100) + "<color=#1fb2de>%</color> " : Math.Round(value, 2) + " ");
    }
}