using Systems.Attakers;
using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct MortarStatsComponent : IComponentData
    {
        public float AOE;
        public float ArrivalTime;
        public float ScatterDistance;
        public float2 LastTargetPosition;
        public float2 LastTargetVelocity;

        public MortarStatsComponent(MortarStats mortarStats) : this()
        {
            AOE = mortarStats.AOE;
            ArrivalTime = mortarStats.ArrivalTime;
            ScatterDistance = mortarStats.ScatterDistance;
            LastTargetVelocity = default;
            LastTargetPosition = default;
        }
    }
}