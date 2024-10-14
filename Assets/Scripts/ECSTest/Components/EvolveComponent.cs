using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace ECSTest.Components
{
    public struct EvolveComponent : IComponentData
    {
        public AllEnums.CreepType UnitToEvolveTo;
        public float PecrentOfHpAndMass;
        public float PercentOfReward;
        public float Timer;
    }
}
