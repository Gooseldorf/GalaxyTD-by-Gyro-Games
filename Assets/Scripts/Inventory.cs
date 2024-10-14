using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Inventory
{
    [SerializeField] private List<WeaponPart> weaponParts = new ();
    [SerializeField] private List<WeaponPart> ammoParts = new ();
    [OdinSerialize,NonSerialized] private Dictionary <WeaponPart, int> directives = new ();

    public IReadOnlyList<IWeaponPart> UnusedWeaponParts => weaponParts;
    public List<WeaponPart> UnusedAmmoParts => ammoParts;

    public Dictionary<WeaponPart, int> UnusedDirectives => directives;

    public void AddWeaponPart(WeaponPart weaponPart)
    {
        if (weaponPart.PartType == AllEnums.PartType.Directive)
        {
            if (directives.ContainsKey(weaponPart))
            {
                directives[weaponPart]++;
                return;
            }
            directives.Add(weaponPart, 1);
        }
        else if (weaponPart.PartType == AllEnums.PartType.Ammo)
        {
            if(!ammoParts.Contains(weaponPart))
                ammoParts.Add(weaponPart);
        }
        else
        {
            if(!weaponParts.Contains(weaponPart))
                weaponParts.Add(weaponPart);
        }
    }

    public void RemoveDirectiveFromInventory(WeaponPart directive)
    {
        if (!directives.ContainsKey(directive))
        {
            Debug.LogWarning($"There is no {directive.name} in inventory");
            return;
        }

        directives[directive]--;
        
        if (directives[directive] <= 0)
            directives.Remove(directive);
        DataManager.Instance.GameData.SaveToDisk();
    }

    public void RemoveAmmoPartFromInventory(WeaponPart ammoPart)
    {
        if (!ammoParts.Contains(ammoPart))
        {
            Debug.LogWarning($"There is no {ammoPart.name} in inventory");
            return;
        }

        ammoParts.Remove(ammoPart);
    }

    public Inventory Clone()
    {
        Inventory clone = new ();
        clone.weaponParts = new List<WeaponPart>(weaponParts);
        clone.ammoParts = new List<WeaponPart>(ammoParts);
        
        clone.directives = new Dictionary<WeaponPart, int>(directives);
        return clone;
    }
}


