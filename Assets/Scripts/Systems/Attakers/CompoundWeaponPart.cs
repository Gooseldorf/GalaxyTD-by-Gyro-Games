using System;
using System.Collections.Generic;

[Serializable]
public abstract class CompoundWeaponPart : WeaponPart//, ICloneable
{
    public abstract void Init(List<Slot> directives, int index);

    /*public virtual object Clone()
    {
        CompoundWeaponPart clone = CreateInstance(GetType()) as CompoundWeaponPart;

        clone.Bonuses = this.Bonuses;
        clone.HardCost = this.HardCost;
        clone.SoftCost = this.SoftCost;
        clone.ScrapCost = this.ScrapCost;

        return clone;
    }*/
}