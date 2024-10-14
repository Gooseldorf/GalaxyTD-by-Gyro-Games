using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Tags;
using UnityEngine;
using static AllEnums;

[Serializable]
public class TowerFactory : ITowerFactory
{
    private const float baseSellModifier = 0.7f;
    
    [OdinSerialize, NonSerialized, OnValueChanged("InitSlots")] private TowerPrototype towerPrototype;
    [OdinSerialize, NonSerialized] private Dictionary<MenuUpgrade, int> menuUpgrades = new();
    [SerializeField, TableList] private List<Slot> parts;
    [SerializeField] private Slot ammo;
    [SerializeField, TableList] private List<Slot> directives;

    public TowerPrototype TowerPrototype => towerPrototype;

    public IReadOnlyList<ISlot> Parts => parts;
    public ISlot Ammo => ammo;
    public IReadOnlyList<ISlot> Directives => directives;
    public TowerId TowerId => TowerPrototype.TowerId;
    public MenuUpgrade NextUpgrade { get; private set; }
    public int Level
    {
        get
        {
            int level = 0;
            foreach (MenuUpgrade key in menuUpgrades.Keys)
            {
                level += menuUpgrades[key];
            }
            return level;
        }
    }

    private List<Slot> directivesActivation;
    private Tower baseTower;
    private Tower assembledTower;
    private bool towerIsInvalidated = true;

    public void AddDirective(WeaponPart part,int index)
    {
        directives[index] = new Slot(PartType.Directive, part);
        RefreshTower();
    }
    
    public void InitFactory(TowerPrototype towerPrototype)
    {
        this.towerPrototype = towerPrototype;
        InitSlots();
    }

    private void InitSlots()
    {
        //PartType partsTypes = PartType.Barrel |
        //    PartType.TargetingSystem |
        //    PartType.RecoilSystem |
        //    PartType.Magazine;

        parts = new List<Slot>();
        foreach (var slotType in TowerPrototype.Parts)
            parts.Add(new Slot(slotType));

        ammo = new Slot(PartType.Ammo);

        directives = new List<Slot>();

        for (int i = 0; i < 4; i++)
            directives.Add(new Slot(PartType.Directive));
    }

    public Tower GetAssembledTower()
    {
        if (assembledTower == null || towerIsInvalidated)
            RefreshTower();
        return assembledTower;
    }

    /// <summary>
    /// Needed for UI to show baseline stats vs assembled tower stats 
    /// </summary>
    /// <returns> base tower</returns>
    public Tower GetBaseTower()
    {
        if (baseTower == null || towerIsInvalidated)
            RefreshTower();
        return baseTower;
    }

    public void RefreshTower()
    {
        AttackStats stats = TowerPrototype.CloneStats;

        if (stats is GunStats)
        {
            switch (TowerPrototype.TowerId)
            {
                case TowerId.TwinGun: assembledTower = new TwinTower(); break;
                case TowerId.Gatling: assembledTower = new GatlingTower(); break;
                default: assembledTower = new GunTower(); break;
            }
        }
        else if (stats is RocketStats)
            assembledTower = new Tower();
        else
        {
            assembledTower = new Tower();
        }

        assembledTower.TowerId = TowerPrototype.TowerId;
        assembledTower.BuildCost = TowerPrototype.BuildCost;
        //stats are cloned
        assembledTower.AttackStats = stats;
        assembledTower.AttackStats.Sellmodifier = baseSellModifier;
        assembledTower.StartOffset = TowerPrototype.StartOffset;
        assembledTower.DamageModifiers = towerPrototype.CloneDamageModifiers;

        //Upgrades
        if (menuUpgrades != null)
            foreach (MenuUpgrade key in menuUpgrades.Keys)
                for (int i = 0; i < menuUpgrades[key]; ++i)
                    key.ApplyStats(assembledTower);

        //needed for UI
        baseTower = assembledTower.Clone() as Tower;

        //Parts 
        List<IStaticTag> partsTags = new ();
        for (int i = 0; i < Parts.Count; i++)
            if (Parts[i].WeaponPart != null)
                for (int j = 0; j < Parts[i].WeaponPart.Bonuses.Count; j++)
                    if (Parts[i].WeaponPart.Bonuses[j] is IStaticTag tag)
                    {
                        partsTags.Add(tag);
                        //tag.ApplyStats(assembledTower);
                    }

        partsTags.Sort(TagsComparer);
        for (int i = 0; i < partsTags.Count; i++)
        {
            //Debug.Log(partsTags[i]);
            partsTags[i].ApplyStats(assembledTower);
        }

        // Ammo
        if (Ammo.WeaponPart != null)
            for (int j = 0; j < Ammo.WeaponPart.Bonuses.Count; j++)
                if (Ammo.WeaponPart.Bonuses[j] is IStaticTag tag)
                    tag.ApplyStats(assembledTower);

        //Directives
        for (int i = 0; i < directives.Count; i++)
            if (directives[i].WeaponPart != null)
                if (directives[i].WeaponPart is CompoundWeaponPart part)
                {
                    part.Init(directives, i);
                }

        directivesActivation = new List<Slot>();
        foreach (var directive in directives)
        {
            directivesActivation.Add(directive);
        }

        directivesActivation.Sort(); //sorting to use IStaticTag tags with greater OrderId last

        for (int i = 0; i < directivesActivation.Count; i++)
            if (directivesActivation[i].WeaponPart != null)
                for (int j = 0; j < directivesActivation[i].WeaponPart.Bonuses.Count; j++)
                {
                    if (directivesActivation[i].WeaponPart.Bonuses[j] is IStaticTag tag)
                    {
                        tag.ApplyStats(assembledTower);
                    }
                }
        directivesActivation.Clear();

        //assembledTower.AttackPattern = assembledTower.AttackStats.ShootingStats.GetNextAvailableAttackPattern(AttackPattern.Off);
        assembledTower.Direction = Vector3.up;

        assembledTower.Directives = directives;
        CalculateCostMultipliers(out float buildMultiplier, out float bulletMultiplier);
        assembledTower.CostMultiplier = buildMultiplier;
        assembledTower.AttackStats.ReloadStats.BulletCost *= bulletMultiplier;
        assembledTower.Ammo = Ammo;
        towerIsInvalidated = false;
    }

    private void CalculateCostMultipliers(out float buildMultiplier, out float bulletMultiplier)
    {
        buildMultiplier = 1f;
        bulletMultiplier = 1f;

        foreach (ISlot part in Parts)
            if (part.WeaponPart != null)
            {
                buildMultiplier *= (1f + part.WeaponPart.TowerCostIncrease);
                bulletMultiplier *= (1f + part.WeaponPart.BulletCostIncrease);
            }
        
        if (Ammo.WeaponPart != null)
        {
            buildMultiplier *= (1f + Ammo.WeaponPart.TowerCostIncrease);
            bulletMultiplier *= (1f + Ammo.WeaponPart.BulletCostIncrease);
        }

        foreach (Slot directive in directives)
            if (directive.WeaponPart != null)
            {
                buildMultiplier *= (1f + directive.WeaponPart.TowerCostIncrease);
                bulletMultiplier *= (1f + directive.WeaponPart.BulletCostIncrease);
            }
    }

    public void UpgradeFactory()
    {
        NextUpgrade = DataManager.Instance.Get<UpgradeProvider>().GetNextUpgrade(TowerId, Level);
        if (menuUpgrades.ContainsKey(NextUpgrade))
            menuUpgrades[NextUpgrade] += 1;
        else
            menuUpgrades[NextUpgrade] = 1;
        towerIsInvalidated = true;
        RefreshTower();
    }

    public TowerFactory Clone()
    {
        Dictionary<MenuUpgrade, int> menuUpgradesClone = new();
        foreach (KeyValuePair<MenuUpgrade, int> menuUpgrade in menuUpgrades)
        {
            MenuUpgrade upgrade = ScriptableObject.CreateInstance<MenuUpgrade>();
            upgrade.IsPercent = menuUpgrade.Key.IsPercent;
            upgrade.AttackStats = menuUpgrade.Key.AttackStats;
            menuUpgradesClone.Add(upgrade, menuUpgrade.Value);
        }

        List<Slot> partsClone = new(parts.Count);
        foreach (Slot part in parts)
            partsClone.Add(new Slot(part.PartType) { WeaponPart = part.WeaponPart});

        List<Slot> directivesClone = new(directives.Count);
        foreach (Slot directive in directives)
            directivesClone.Add(new Slot(directive.PartType) { WeaponPart = directive.WeaponPart});
        
        return new()
        {
            towerPrototype = towerPrototype,
            menuUpgrades = menuUpgradesClone,
            parts = partsClone,
            ammo = new Slot(ammo.PartType) { WeaponPart = ammo.WeaponPart },
            directives = directivesClone,
            assembledTower = assembledTower,
            towerIsInvalidated = towerIsInvalidated,
            baseTower = baseTower,
        };
    }

    private int TagsComparer(IStaticTag a, IStaticTag b) => a.OrderId.CompareTo(b.OrderId);
}