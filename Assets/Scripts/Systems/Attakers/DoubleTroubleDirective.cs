using I2.Loc;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DoubleTroubleDirective : CompoundWeaponPart
{
    [SerializeField, PropertyOrder(-1)] private float baseCostIncrease = 0.15f;
    [SerializeField, PropertyOrder(-1)] private List<WeaponPart> blackList;

    [NonSerialized] private List<WeaponPart> parts = new();
    
    public override void Init(List<Slot> directives, int index)
    {
        parts.Clear();
        Bonuses.Clear();
        TowerCostIncrease = baseCostIncrease;

        foreach (var directive in directives)
        {
            if(directive.WeaponPart != null)
                if (!blackList.Contains(directive.WeaponPart))
                {
                    parts.Add(directive.WeaponPart);
                    TowerCostIncrease += directive.WeaponPart.TowerCostIncrease;
                }
        }

        foreach (WeaponPart part in parts)
            Bonuses.AddRange(part.Bonuses);
        
        //TODO and unique visual?
    }

    public override string GetDescription()=> "<color=#1fb2de>></color> " + LocalizationManager.GetTranslation("Tags/DoubleTrouble");
}