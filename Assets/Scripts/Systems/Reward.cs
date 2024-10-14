using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using static AllEnums;

[Serializable]
public class Reward
{
    public int SoftCurrency;
    public int HardCurrency;
    public int Scrap;
    public int Dust;
    
    //recieve immediately
    public WeaponPart WeaponPart = null;
    //unlock for buying ?
    public List<WeaponPart> PartsToUnlock = null;
    public TowerId Tower;
    public Reward()
    {
        SoftCurrency = 0;
        HardCurrency = 0;
        Scrap = 0;
        Dust = 0;
        WeaponPart = null;
        PartsToUnlock = null;
        Tower = 0;
    }
}