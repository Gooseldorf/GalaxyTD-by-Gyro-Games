using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace ECSTest.Components
{
    public struct SpawnerComponent : IComponentData
    {
        public AllEnums.CreepType UnitToSpawn;
        public int SpawnUnitsCount;
        public float PecrentOfHpAndMass;
        public float PercentOfReward;
        public float TimeBetweenSpawn;
        public float Timer;
    }
}
