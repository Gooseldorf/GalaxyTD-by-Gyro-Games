using I2.Loc;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public sealed class RandomDirective : CompoundWeaponPart
{
    [SerializeField, PropertyOrder(-1)] private List<WeaponPart> directivesToMorph;

    [NonSerialized] private List<WeaponPart> parts = new();
    
    public override void Init(List<Slot> directives, int index)
    {
        parts.Clear();
        int rand = Random.Range(0, directivesToMorph.Count);
        parts.Add(directivesToMorph[rand]);
        
        Bonuses.Clear();
        Bonuses.AddRange(parts[0].Bonuses);
    }

    public override string GetDescription()=>   "<color=#1fb2de>></color> " + LocalizationManager.GetTranslation("Tags/RandomDirective");
}