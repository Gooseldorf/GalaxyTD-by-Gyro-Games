using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

namespace TestingAgent
{
    [Serializable]
    public sealed class WaveData
    {
        public CreepStats Stats;
        public List<WaveDateEntry> Entries;

        [Serializable]
        public sealed class WaveDateEntry
        {
            public int Count;
            public bool OverrideCreepHp;
        
            [Indent, HideIf("@OverrideCreepHp==false")] 
            public float CreepHp;
        }
    }
}