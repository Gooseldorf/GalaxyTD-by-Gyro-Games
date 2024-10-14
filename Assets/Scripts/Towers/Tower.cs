using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Tower : IAttacker, ICloneable
{
    [OdinSerialize] private int buildCost;

    public AllEnums.TowerId TowerId;
    [OdinSerialize] public float CostMultiplier;
    public int Id { get; set; }
    public float3 Position { get; set; }
    public DamageModifiers DamageModifiers { get; set; }
    [OdinSerialize] public IReadOnlyList<ISlot> Directives { get; set; }
    [OdinSerialize] public ISlot Ammo { get; set; }
    [ShowInInspector] public float3 Direction { get; set; }
    [ShowInInspector, OdinSerialize] public AttackStats AttackStats { get; set; }
    [ShowInInspector, HorizontalGroup, HideLabel] public float AttackDelay { get; set; }
    [OdinSerialize] public virtual float StartOffset { get; set; }
    [OdinSerialize] public bool AutoReload { get; set; } = true;

    public int BuildCost
    {
        get => Mathf.RoundToInt(buildCost * CostMultiplier);
        set => buildCost = value;
    }

    public object Clone()
    {
        //TODO: proper clone
        Tower result = (Tower)MemberwiseClone();
        result.AttackStats = (AttackStats)AttackStats.Clone();
        return result;
    }
}