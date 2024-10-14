using System;
using System.Collections.Generic;
using I2.Loc;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public class TowerPrototype : SerializedScriptableObject, ICustomSerialized, ILocalized
{

    [field: SerializeField] public Sprite Sprite { get; private set; }
    [field: SerializeField] public AllEnums.TowerId TowerId { get; private set; }
    [field: SerializeField] public int BuildCost { get; private set; }
    /// <summary>For CSVParser only </summary>
    public void SetBuildCostDirty(int value) => BuildCost = value;

    [OdinSerialize, NonSerialized] private AttackStats attackStats;
    [OdinSerialize, NonSerialized] private DamageModifiers damageModifiers;
    
    public AttackStats CloneStats => Stats.Clone() as AttackStats;
    public DamageModifiers CloneDamageModifiers => damageModifiers;
    [field: SerializeField] public List<AllEnums.PartType> Parts { get; private set; }
    public string SerializedID => TowerId.ToString();
    [field: SerializeField] public float StartOffset { get; private set; }

    public AttackStats Stats
    {
        get => attackStats;
        set => attackStats = value;
    }
    
    public string GetTitle()
    {
        return LocalizationManager.GetTranslation($"Tower/{TowerId}_title").ToUpper();
    }

    public string GetDescription()
    {
        return LocalizationManager.GetTranslation($"Tower/{TowerId}_desc");
    }
}
