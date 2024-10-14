using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace ECSTest.Components
{
    public struct SpawnComponent : IComponentData, IEnableableComponent
    {
        public float SpawnTime;
    }
}
