using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace ECSTest.Components
{
    public struct ProximityWarningEvent : IComponentData
    {
        public Entity EnergyCore;
        public bool HasWarning;
    }
}
