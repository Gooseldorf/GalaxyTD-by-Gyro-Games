using System;
using UnityEngine;

namespace ABS
{
    [Serializable]
    public class NamingProperty : PropertyBase
    {
        public string fileNamePrefix = "";
        public bool isModelPrefixSprite = true;
    }
}
