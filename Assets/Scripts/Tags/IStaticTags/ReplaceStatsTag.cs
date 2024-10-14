using ECSTest.Components;
using Sirenix.Serialization;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using I2.Loc;
using Systems.Attakers;
using Unity.Entities;

public sealed class ReplaceStatsTag : Tag, IStaticTag
{
    [OdinSerialize, PropertyOrder(-1)]
    public int OrderId { get; set; } = -1;

    [SerializeField, EnumToggleButtons]
    private ReplaceType replaceType;

    [SerializeField]
    [ShowIf("@replaceType.HasFlag(ReplaceType.FireMode)")]
    private AllEnums.AttackPattern availableAttackPatterns;

    [SerializeField]
    [ShowIf("@replaceType.HasFlag(ReplaceType.KnockBack)")]
    private float knockbackValue;

    [SerializeField]
    [ShowIf("@replaceType.HasFlag(ReplaceType.Recoil)")]
    private float recoilValue;

    [SerializeField]
    [ShowIf("@replaceType.HasFlag(ReplaceType.Deviation)")]
    private float deviationValue;

    [SerializeField]
    [ShowIf("@replaceType.HasFlag(ReplaceType.Control)")]
    private float controlValue;

    [SerializeField]
    [ShowIf("@replaceType.HasFlag(ReplaceType.MagazineSize)")]
    private float rawMagazineSizeValue;

    [SerializeField]
    [ShowIf("@replaceType.HasFlag(ReplaceType.Range)")]
    private float rangeValue;

    [SerializeField] 
    [ShowIf("@replaceType.HasFlag(ReplaceType.ScatterDistance)")]
    private float scatterDistanceValue;
    
    [SerializeField] 
    [ShowIf("@replaceType.HasFlag(ReplaceType.ProjectilesPerShot)")]
    private int projectilesPerShotValue;

    public void ApplyStats(Tower tower)
    {
        if (replaceType.HasFlag(ReplaceType.FireMode))
            tower.AttackStats.ShootingStats.AvailableAttackPatterns = availableAttackPatterns;

        if (replaceType.HasFlag(ReplaceType.KnockBack))
            tower.AttackStats.KnockBackPerBullet = knockbackValue;

        if (replaceType.HasFlag(ReplaceType.MagazineSize))
            tower.AttackStats.ReloadStats.RawMagazineSize = rawMagazineSizeValue;

        if (replaceType.HasFlag(ReplaceType.Range))
            tower.AttackStats.AimingStats.Range = rangeValue;
        
        if (tower.AttackStats is GunStats gunStats)
        {
            if (replaceType.HasFlag(ReplaceType.Recoil))
                gunStats.AccuracyStats.Recoil = recoilValue;
            
            if (replaceType.HasFlag(ReplaceType.Deviation))
                gunStats.AccuracyStats.Deviation = deviationValue;
            
            if (replaceType.HasFlag(ReplaceType.Control))
                gunStats.AccuracyStats.Control = controlValue;
        }

        if (replaceType.HasFlag(ReplaceType.ScatterDistance))
        {
            if (tower.AttackStats is MortarStats mortarStats)
                mortarStats.ScatterDistance = scatterDistanceValue;
            
            if (tower.AttackStats is RocketStats rocketStats)
                rocketStats.ScatterDistance = scatterDistanceValue;
        }

        if (replaceType.HasFlag(ReplaceType.ProjectilesPerShot))
            tower.AttackStats.ShootingStats.ProjectilesPerShot = projectilesPerShotValue;
    }

    public void ApplyStats(Entity towerEntity, EntityManager manager)
    {
        var attacker = manager.GetComponentData<AttackerComponent>(towerEntity);
        
        if (replaceType.HasFlag(ReplaceType.FireMode))
            attacker.AttackStats.ShootingStats.AvailableAttackPatterns = availableAttackPatterns;

        if (replaceType.HasFlag(ReplaceType.KnockBack))
            attacker.AttackStats.KnockBackPerBullet = knockbackValue;

        if (replaceType.HasFlag(ReplaceType.MagazineSize))
            attacker.AttackStats.ReloadStats.RawMagazineSize = rawMagazineSizeValue;

        if (replaceType.HasFlag(ReplaceType.Range))
            attacker.AttackStats.AimingStats.Range = rangeValue;

        if (manager.HasComponent<GunStatsComponent>(towerEntity))
        {
            var gunStats = manager.GetComponentData<GunStatsComponent>(towerEntity);
            
            if (replaceType.HasFlag(ReplaceType.Recoil))
                gunStats.Recoil = recoilValue;
            
            if (replaceType.HasFlag(ReplaceType.Deviation))
                gunStats.Deviation = deviationValue;
            
            if (replaceType.HasFlag(ReplaceType.Control))
                gunStats.Control = controlValue;
            
            manager.SetComponentData(towerEntity,gunStats);
        }

        if (replaceType.HasFlag(ReplaceType.ScatterDistance))
        {
            if (manager.HasComponent<MortarStatsComponent>(towerEntity))
            {
                var mortarStatsComponent = manager.GetComponentData<MortarStatsComponent>(towerEntity);
                mortarStatsComponent.ScatterDistance = scatterDistanceValue;
                manager.SetComponentData(towerEntity,mortarStatsComponent);
            }
            
            if (manager.HasComponent<RocketStatsComponent>(towerEntity))
            {
                var rocketStatsComponent = manager.GetComponentData<RocketStatsComponent>(towerEntity);
                rocketStatsComponent.ScatterDistance = scatterDistanceValue;
                manager.SetComponentData(towerEntity,rocketStatsComponent);
            }
        }

        if (replaceType.HasFlag(ReplaceType.ProjectilesPerShot))
            attacker.AttackStats.ShootingStats.ProjectilesPerShot = projectilesPerShotValue;
        
        manager.SetComponentData(towerEntity,attacker);
    }

    public override string GetDescription()
    {
        string result = string.Empty;

        if (replaceType.HasFlag(ReplaceType.KnockBack))
        {
            if (!string.IsNullOrEmpty(result)) result += "\n";
            result += LocalizationManager.GetTranslation("Tags/ReplaceStats")
                        .Replace("{param1}", LocalizationManager.GetTranslation("TowerStats/KnockBack"))
                        .Replace("{param2}", knockbackValue.ToString());
        }

        if (replaceType.HasFlag(ReplaceType.MagazineSize))
        {
            if (!string.IsNullOrEmpty(result)) result += "\n";
            result += LocalizationManager.GetTranslation("Tags/ReplaceStats")
                        .Replace("{param1}", LocalizationManager.GetTranslation("TowerStats/MagazineSize"))
                        .Replace("{param2}", ((int)rawMagazineSizeValue).ToString());
        }

        if (replaceType.HasFlag(ReplaceType.Range))
        {
            if (!string.IsNullOrEmpty(result)) result += "\n";
            result += LocalizationManager.GetTranslation("Tags/ReplaceStats")
                        .Replace("{param1}", LocalizationManager.GetTranslation("TowerStats/Range"))
                        .Replace("{param2}", ((int)rangeValue).ToString());
        }

        if (replaceType.HasFlag(ReplaceType.Recoil))
        {
            if (!string.IsNullOrEmpty(result)) result += "\n";
            result += LocalizationManager.GetTranslation("Tags/ReplaceStats")
                        .Replace("{param1}", LocalizationManager.GetTranslation("TowerStats/Recoil"))
                        .Replace("{param2}", recoilValue.ToString());
        }

        if (replaceType.HasFlag(ReplaceType.Deviation))
        {
            if (!string.IsNullOrEmpty(result)) result += "\n";
            result += LocalizationManager.GetTranslation("Tags/ReplaceStats")
                        .Replace("{param1}", LocalizationManager.GetTranslation("TowerStats/Deviation"))
                        .Replace("{param2}", deviationValue.ToString());
        }

        if (replaceType.HasFlag(ReplaceType.Control))
        {
            if (!string.IsNullOrEmpty(result)) result += "\n";
            result += LocalizationManager.GetTranslation("Tags/ReplaceStats")
                        .Replace("{param1}", LocalizationManager.GetTranslation("TowerStats/Control"))
                        .Replace("{param2}", controlValue.ToString());
        }
        
        if (replaceType.HasFlag(ReplaceType.ScatterDistance))
        {
            if (!string.IsNullOrEmpty(result)) result += "\n";
            result += LocalizationManager.GetTranslation("Tags/ReplaceStats")
                .Replace("{param1}", LocalizationManager.GetTranslation("TowerStats/ScatterDistance"))
                .Replace("{param2}", scatterDistanceValue.ToString());
        }
        
        if (replaceType.HasFlag(ReplaceType.ProjectilesPerShot))
        {
            if (!string.IsNullOrEmpty(result)) result += "\n";
            result += LocalizationManager.GetTranslation("Tags/ReplaceStats")
                .Replace("{param1}", LocalizationManager.GetTranslation("TowerStats/ProjectilesPerShot"))
                .Replace("{param2}", projectilesPerShotValue.ToString());
        }

        return result;
    }

    public bool TryGetAttackPatterns(out AllEnums.AttackPattern patterns)
    {
        if (replaceType.HasFlag(ReplaceType.FireMode))
        {
            patterns = availableAttackPatterns;
            return true;
        }
        else
        {
            patterns = AllEnums.AttackPattern.Off;
            return false;
        }
    }

    [System.Flags]
    public enum ReplaceType
    {
        FireMode = 1 << 1,
        KnockBack = 1 << 2,
        Recoil = 1 << 3,
        Deviation = 1 << 4,
        Control = 1 << 5,
        MagazineSize = 1 << 6,
        Range = 1 << 7,
        ScatterDistance = 1 << 8,
        ProjectilesPerShot = 1 << 9
    }
}
