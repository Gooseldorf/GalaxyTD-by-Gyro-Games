using I2.Loc;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class MirrorDirective : CompoundWeaponPart
{
    [SerializeField, PropertyOrder(-1)] private float baseCostIncrease = 0.15f;
    [SerializeField, PropertyOrder(-1), InfoBox("Number of upper slots with directives")] private int numberOfSteps = 1;
    [SerializeField, PropertyOrder(-1)] private int numberOfCopies = 1;
    [SerializeField, PropertyOrder(-1)] private List<WeaponPart> blackList;

    [NonSerialized] private List<WeaponPart> parts = new();

    public override void Init(List<Slot> directives, int index)
    {
        parts.Clear();
        Bonuses.Clear();
        TowerCostIncrease = baseCostIncrease;
        int startIndex = index - 1;

        for (int i = 0; i < numberOfSteps; i++)
        {
            int indexToAdd = startIndex - i;
            if (indexToAdd < 0 || indexToAdd >= directives.Count) break;

            if (directives[indexToAdd].WeaponPart != null)
                if (!blackList.Contains(directives[indexToAdd].WeaponPart))
                    for (int j = 0; j < numberOfCopies; j++)
                    {
                        parts.Add(directives[indexToAdd].WeaponPart);
                        TowerCostIncrease += directives[indexToAdd].WeaponPart.TowerCostIncrease;
                    }
        }

        foreach (WeaponPart part in parts)
            Bonuses.AddRange(part.Bonuses);
    }

    public override string GetDescription()
    {
        string result = LocalizationManager.GetTranslation($"Tags/{name}");

        if (numberOfSteps != 1)
            result = result.Replace("{param}", numberOfSteps.ToString());

        return  "<color=#1fb2de>></color> " + result;
    }
}