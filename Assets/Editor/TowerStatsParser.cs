#if UNITY_EDITOR

using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Systems.Attakers;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class TowerStatsParser : SerializedScriptableObject
{
    [SerializeField] private UpgradeProvider upgradeProvider;
    [SerializeField] private GameData gameData;

    [FoldoutGroup("Tower Stats Result")][OdinSerialize, NonSerialized, ShowInInspector] private List<AttackStats> towerStats;
    [FoldoutGroup("Game Upgrade Costs Result")][OdinSerialize, NonSerialized, ShowInInspector] private Dictionary<AllEnums.TowerId, List<int>> gameUpgradeCosts;
    [OdinSerialize, NonSerialized, ShowInInspector] private Dictionary<AllEnums.TowerId, int> towerDataPositions;

    private string result;
    private const int offset = 26;
    private const int maxTowerLvl = 15;

    [InfoBox("Before parsing, copy tower effectiveness table to clipboard!!!")]

    [Button]
    public void UpdatePrototypesAndUpgradeCosts()
    {
        result = GUIUtility.systemCopyBuffer;
        if (result == String.Empty)
        {
            Debug.LogError("Clipboard is empty!");
        }
        result = result.Replace("\t\t", "\t-1\t");
        result = result.Replace(',', '.');

        result = Regex.Replace(result, @"(\d+)%", m => (int.Parse(m.Groups[1].Value) / 100.0).ToString());

        string[] allLines = result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        List<string[]> splitData = new();
        
        foreach (string line in allLines)
        {
            string[] split = line.Split('\t');
            if (split.Length > 1)
                splitData.Add(split);
        }

        towerStats = new();
        gameUpgradeCosts = new();

        List<TowerPrototype> towerPrototypes = new();

        foreach (var factory in gameData.TowerFactories)
        {
            towerPrototypes.Add(((TowerFactory)factory).TowerPrototype);
        }

        EditorUtility.SetDirty(upgradeProvider);
        foreach (var pair in towerDataPositions)
        {
            TowerGameUpgrades gameUpgrade = upgradeProvider.TowerGameUpgradesList.Find(x => x.TowerId == pair.Key);
            TowerPrototype prototype = towerPrototypes.Find(x => x.TowerId == pair.Key);
            UpdateTowerPrototype(prototype, pair.Value, splitData);
            towerStats.Add(prototype.CloneStats);

            gameUpgradeCosts.Add(pair.Key, new List<int>(maxTowerLvl));

            gameUpgrade.Costs = new List<int>(maxTowerLvl);
            gameUpgrade.AmmoCostMult = new List<float>(maxTowerLvl);
            for (int i = 1; i < splitData.Count && i <= maxTowerLvl; i++)
            {
                gameUpgrade.Costs.Add((int)float.Parse(splitData[i][pair.Value * offset + 19], CultureInfo.InvariantCulture));
                gameUpgradeCosts[pair.Key].Add((int)float.Parse(splitData[i][pair.Value * offset + 19], CultureInfo.InvariantCulture));
                gameUpgrade.AmmoCostMult.Add(float.Parse(splitData[i][pair.Value * offset + 24], CultureInfo.InvariantCulture));
            }
        }
        AssetDatabase.SaveAssets();
    }

    private void UpdateTowerPrototype(TowerPrototype prototype, int positionInData, List<string[]> splitData)
    {
        EditorUtility.SetDirty(prototype);

        prototype.SetBuildCostDirty((int)float.Parse(splitData[0][positionInData * offset + 18], CultureInfo.InvariantCulture));
        // for burst
        float averageShootDelay;

        switch (prototype.TowerId)
        {
            case AllEnums.TowerId.Mortar:
                MortarStats mStats = prototype.Stats as MortarStats;
                mStats.DamagePerBullet = float.Parse(splitData[0][positionInData * offset + 6], CultureInfo.InvariantCulture) / mStats.ShootingStats.ProjectilesPerShot;
                mStats.AimingStats.Range = float.Parse(splitData[0][positionInData * offset + 9], CultureInfo.InvariantCulture);
                mStats.ReloadStats.RawMagazineSize = float.Parse(splitData[0][positionInData * offset + 10], CultureInfo.InvariantCulture);
                mStats.ShootingStats.ShotDelay = float.Parse(splitData[0][positionInData * offset + 11], CultureInfo.InvariantCulture);
                mStats.ReloadStats.ReloadTime = float.Parse(splitData[0][positionInData * offset + 12], CultureInfo.InvariantCulture);
                mStats.ScatterDistance = float.Parse(splitData[0][positionInData * offset + 15], CultureInfo.InvariantCulture);
                mStats.KnockBackPerBullet = float.Parse(splitData[0][positionInData * offset + 16], CultureInfo.InvariantCulture);
                mStats.AOE = float.Parse(splitData[0][positionInData * offset + 17], CultureInfo.InvariantCulture);
                mStats.ReloadStats.BulletCost = float.Parse(splitData[0][positionInData * offset + 23], CultureInfo.InvariantCulture);
                break;
            case AllEnums.TowerId.Rocket:
                RocketStats rStats = prototype.Stats as RocketStats;
                rStats.DamagePerBullet = float.Parse(splitData[0][positionInData * offset + 6], CultureInfo.InvariantCulture) / rStats.ShootingStats.ProjectilesPerShot;
                rStats.AimingStats.Range = float.Parse(splitData[0][positionInData * offset + 9], CultureInfo.InvariantCulture);
                rStats.ReloadStats.RawMagazineSize = float.Parse(splitData[0][positionInData * offset + 10], CultureInfo.InvariantCulture);
                rStats.ShootingStats.ShotDelay = float.Parse(splitData[0][positionInData * offset + 11], CultureInfo.InvariantCulture);
                rStats.ReloadStats.ReloadTime = float.Parse(splitData[0][positionInData * offset + 12], CultureInfo.InvariantCulture);

                float averageShootDelay2 = float.Parse(splitData[0][positionInData * offset + 13], CultureInfo.InvariantCulture);

                rStats.ScatterDistance = float.Parse(splitData[0][positionInData * offset + 15], CultureInfo.InvariantCulture);
                rStats.KnockBackPerBullet = float.Parse(splitData[0][positionInData * offset + 16], CultureInfo.InvariantCulture);
                rStats.AOE = float.Parse(splitData[0][positionInData * offset + 17], CultureInfo.InvariantCulture);
                rStats.ReloadStats.BulletCost = float.Parse(splitData[0][positionInData * offset + 23], CultureInfo.InvariantCulture);
                break;
            default:
                GunStats stats = prototype.Stats as GunStats;
                stats.DamagePerBullet = float.Parse(splitData[0][positionInData * offset + 4], CultureInfo.InvariantCulture) / stats.ShootingStats.ProjectilesPerShot;
                stats.AimingStats.Range = float.Parse(splitData[0][positionInData * offset + 6], CultureInfo.InvariantCulture);
                stats.ReloadStats.RawMagazineSize = float.Parse(splitData[0][positionInData * offset + 7], CultureInfo.InvariantCulture);
                stats.ShootingStats.ShotDelay = float.Parse(splitData[0][positionInData * offset + 8], CultureInfo.InvariantCulture);
                stats.ReloadStats.ReloadTime = float.Parse(splitData[0][positionInData * offset + 9], CultureInfo.InvariantCulture);

                averageShootDelay = float.Parse(splitData[0][positionInData * offset + 10], CultureInfo.InvariantCulture);

                stats.AccuracyStats.Deviation = float.Parse(splitData[0][positionInData * offset + 12], CultureInfo.InvariantCulture) * math.TODEGREES;
                stats.KnockBackPerBullet = float.Parse(splitData[0][positionInData * offset + 13], CultureInfo.InvariantCulture);
                stats.RicochetStats.RicochetCount = Mathf.RoundToInt(float.Parse(splitData[0][positionInData * offset + 14], CultureInfo.InvariantCulture));
                stats.RicochetStats.PenetrationCount = Mathf.RoundToInt(float.Parse(splitData[0][positionInData * offset + 15], CultureInfo.InvariantCulture));
                stats.RicochetStats.DamageMultPerRicochet = float.Parse(splitData[0][positionInData * offset + 16], CultureInfo.InvariantCulture);
                stats.RicochetStats.DamageMultPerPenetration = float.Parse(splitData[0][positionInData * offset + 17], CultureInfo.InvariantCulture);
                stats.ReloadStats.BulletCost = float.Parse(splitData[0][positionInData * offset + 23], CultureInfo.InvariantCulture);
                break;
        }
    }
}
#endif
