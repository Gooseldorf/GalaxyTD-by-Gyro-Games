using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace ECSTest.Components
{
    public struct HydraComponent : IComponentData
    {
        public AllEnums.CreepType UnitToSpawn;
        public int SpawnUnitsCount;
        public float PecrentOfHpAndMass;
        public float PercentOfReward;
        public bool WasSpawned;
    }
}
