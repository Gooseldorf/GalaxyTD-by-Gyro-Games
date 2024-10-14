using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace ECSTest.Components
{
    public struct FearComponent : IComponentData
    {
        public float Time;
    }
}
