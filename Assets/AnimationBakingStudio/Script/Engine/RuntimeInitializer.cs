using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    [DisallowMultipleComponent]
    public abstract class RuntimeInitializer : MonoBehaviour
    {
        public abstract void Initialize();
    }
}
