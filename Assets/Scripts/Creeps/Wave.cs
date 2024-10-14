using Sirenix.Serialization;
using System;
using System.Collections.Generic;

[Serializable]
public class Wave
{
    public int WaveNum;
    public int CreepHp;
    public int CashReward;
    // [OdinSerialize, NonSerialized]
    // public Dictionary<CreepStats, int> SpawnUnits;
    //public CreepStats Creep;
    public int Count;
    public float ExtraTime = 0;

}