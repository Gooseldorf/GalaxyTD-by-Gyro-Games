using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace ECSTest.Systems
{
    [Serializable]
    public class PairList
    {
        public int2 StartPosition;
        public Dictionary<int2, HashSet<int2>> Dictionary = new();

        public PairList Clone()
        {
            PairList clone = new();
            clone.StartPosition = StartPosition;
            clone.Dictionary = new Dictionary<int2, HashSet<int2>>(Dictionary);
            return clone;
        }
    }
}