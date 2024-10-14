using ECSTest.Components;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using Tags;
using Unity.Entities;
using UnityEngine;
using static AllEnums;

public class UpgradeProvider : SerializedScriptableObject
{
    [FoldoutGroup("MenuUpgrades")][SerializeField] private MenuUpgrade baseMenuUpgrade;
    [FoldoutGroup("MenuUpgrades")][SerializeField] private MenuUpgrade infinityMenuUpgrade;
    [FoldoutGroup("MenuUpgrades")][SerializeField] private int infinityUpgradeCostIncrease;
    [FoldoutGroup("MenuUpgrades")][OdinSerialize, NonSerialized, ShowInInspector] private Dictionary<TowerId, List<int>> costs = new();

    [FoldoutGroup("Game Upgrades")][OdinSerialize, NonSerialized, ShowInInspector] private List<TowerGameUpgrades> towerGameUpgrades;
    [FoldoutGroup("Game Upgrades")] public int GameUpgradeLevelCap => towerGameUpgrades[0].Costs.Count;
    public List<TowerGameUpgrades> TowerGameUpgradesList => towerGameUpgrades;

    [FoldoutGroup("Game Upgrades")][InfoBox("The key represents the level from which the upgrade will be applied")] [OdinSerialize, NonSerialized, ShowInInspector]
    private Dictionary<int, SimpleUpgrade> everyLevelUpgrades;
    
    public SimpleUpgrade Level5Upgrade;
    public MenuUpgrade GetNextUpgrade(TowerId towerId, int currentFactoryLevel)
    {
        List<int> currentCosts = this.costs[towerId];
        if (currentFactoryLevel >= currentCosts.Count)
        {
            int infinityUpgradeCount = currentFactoryLevel - currentCosts.Count + 1;
            infinityMenuUpgrade.Cost = currentCosts[^1] + infinityUpgradeCostIncrease * infinityUpgradeCount;
            return infinityMenuUpgrade;
        }
        else
        {
            baseMenuUpgrade.Cost = currentCosts[currentFactoryLevel];
            return baseMenuUpgrade;
        }
    }

    public bool TryGetNextGameUpgrade(TowerId towerId, int currentTowerLevel, out CompoundUpgrade nextGameUpgrade)
    {
        nextGameUpgrade = null;

        if (currentTowerLevel >= GameUpgradeLevelCap)
            return false;

        TowerGameUpgrades upgrades = towerGameUpgrades.Find(x => x.TowerId == towerId);
        nextGameUpgrade = new CompoundUpgrade(GetEveryLevelUpgrade(currentTowerLevel), upgrades.Costs[currentTowerLevel], upgrades.AmmoCostMult[currentTowerLevel]);
        if (upgrades == null)
            throw new Exception("no upgrades for " + towerId + " found");

        switch (currentTowerLevel)
        {
            case 3:
                nextGameUpgrade.Upgrades.Add(Level5Upgrade);
                break;
            case 8:
                //if (upgrades.Level10Upgrade != null)
                    nextGameUpgrade.Upgrades.Add(upgrades.Level10Upgrade);
                break;
            default:
                break;
        }
        //nextGameUpgrade = currentTowerLevel <= gameUpgradesThresholdLevel ? upgrades.EarlyGameUpgrade : upgrades.LateGameUpgrade;
        return true;
    }
    [Button]
    private SimpleUpgrade GetEveryLevelUpgrade(int currentLevel)
    {
        int result = 0;
        foreach (KeyValuePair<int, SimpleUpgrade> pair in everyLevelUpgrades)
        {
            if (currentLevel + 1 >= pair.Key)
            {
                result = pair.Key;
            }
        }

        return everyLevelUpgrades[result];
    }

    public string GetNextUpgradeDesc(TowerId towerId, int currentFactoryLevel)
    {
        return "<color=#1fb2de>></color> " + GetNextUpgrade(towerId, currentFactoryLevel).GetDescription();
    }

#if UNITY_EDITOR
    [FoldoutGroup("Parse")][SerializeField, TextArea] private string parseArea;
    [FoldoutGroup("Parse")][OdinSerialize, NonSerialized, ShowInInspector] private Dictionary<int, TowerId> towerIndexes;

    [Button]
    private void ParseMenuUpgradeCosts()
    {
        costs ??= new();
        costs?.Clear();

        for (int i = 0; i < towerIndexes.Count; i++)
            costs[towerIndexes[i]] = new();

        string[] data = parseArea.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (string stringData in data)
        {
            string[] costData = stringData.Split('\t', StringSplitOptions.None);
            for (int i = 0; i < towerIndexes.Count; i++)
            {
                if (int.TryParse(costData[i], out int parsedCost))
                    costs[towerIndexes[i]].Add(parsedCost);
                else
                    break;
            }
        }
    }
#endif
}

[Serializable]
public class TowerGameUpgrades
{
    public TowerId TowerId;

    public SimpleUpgrade Level10Upgrade;

    public List<int> Costs;

    public List<float> AmmoCostMult;
}

public class CompoundUpgrade
{
    public List<SimpleUpgrade> Upgrades;
    public int Cost;
    public float AmmoCostMult;

    public CompoundUpgrade(SimpleUpgrade everyLevelUpgrade, int cost, float ammoCostMult)
    {
        Upgrades = new List<SimpleUpgrade> { everyLevelUpgrade };
        Cost = cost;
        AmmoCostMult = ammoCostMult;
    }

    public void ApplyUpgrades(Entity towerEntity, ref EntityManager manager, ref AttackerComponent attackerComponent)
    {
        foreach (SimpleUpgrade upgrade in Upgrades)
        {
            upgrade.ApplyOneBonus(towerEntity, ref manager, ref attackerComponent);
        }
        attackerComponent.AttackStats.ReloadStats.BulletCost *= AmmoCostMult;
        attackerComponent.Bullets = attackerComponent.AttackStats.ReloadStats.MagazineSize;
        attackerComponent.ReloadTimer = 0;
        
        manager.SetComponentData(towerEntity, attackerComponent);
    }

}

