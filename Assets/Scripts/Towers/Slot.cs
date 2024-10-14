using Sirenix.OdinInspector;
using System;
using UnityEngine;

[Serializable]
public class Slot : ISlot, IComparable<Slot>
{
    public Slot(AllEnums.PartType slotType)
    {
        PartType = slotType;
        WeaponPart = null;
    }

    public Slot(AllEnums.PartType slotType,WeaponPart part)
    {
        WeaponPart = part;
        PartType = slotType;
    }

    [field: SerializeField,HorizontalGroup,HideLabel]
    public AllEnums.PartType PartType { get; set; }

    [field: SerializeField, HorizontalGroup, HideLabel]
    public WeaponPart WeaponPart { get; set; }
    
    public int CompareTo(Slot obj)
    {
        Slot a = this;
        Slot b = obj;

        bool aHasStaticTag = false;
        bool bHasStaticTag = false;
        int maxAOrderId = int.MinValue;
        int maxBOrderId = int.MinValue;
        int aBonusesCount = 0;
        int bBonusesCount = 0;

        if (a.WeaponPart == null)
            return 1;

        if (b.WeaponPart == null)
            return -1;
        
        foreach (Tag bonus in a.WeaponPart.Bonuses)
        {
            if (bonus is IStaticTag tag)
            {
                aHasStaticTag = true;
                aBonusesCount++;
                if (tag.OrderId > maxAOrderId)
                {
                    maxAOrderId = tag.OrderId;
                }
            }
        }
        
        if (!aHasStaticTag)
            return 1;

        foreach (Tag bonus in b.WeaponPart.Bonuses)
        {
            if (bonus is IStaticTag tag)
            {
                bHasStaticTag = true;
                bBonusesCount++;
                if (tag.OrderId > maxBOrderId)
                {
                    maxBOrderId = tag.OrderId;
                }
            }
        }

        if (!bHasStaticTag)
            return -1;

        if (maxAOrderId > maxBOrderId)
            return 1;
        if (maxBOrderId > maxAOrderId)
            return -1;
        
        if (maxBOrderId == maxAOrderId)
        {
            if (aBonusesCount > bBonusesCount)
                return -1;
            if (bBonusesCount > aBonusesCount)
                return 1;
        }

        return 0;
    }
}