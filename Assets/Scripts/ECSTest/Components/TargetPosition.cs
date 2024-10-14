using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct TargetPosition : IComponentData
    {
        public float3 value;
    }
}