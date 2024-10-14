using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECSTest.Components
{
    public struct EvolveEvent : IComponentData
    {
        public float2 Position;
    }
}
