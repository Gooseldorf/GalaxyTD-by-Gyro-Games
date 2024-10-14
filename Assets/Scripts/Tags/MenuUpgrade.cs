using System;
using UnityEngine;

namespace Tags
{
    [Serializable]
    public class MenuUpgrade : StatsTag, ICustomSerialized
    {
        [field:SerializeField]
        public int Cost { get; set; }

        public string SerializedID => name;
    }
}