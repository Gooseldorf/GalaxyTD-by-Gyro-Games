using System;

namespace ABS
{
    [Serializable]
    public class TrimmingProperty : PropertyBase
    {
        public bool on = true;
        public int margin = 2;
        public bool isUniformSize = false;

        public bool IsOnUniformSize()
        {
            return on && isUniformSize;
        }
    }
}
