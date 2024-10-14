using Unity.Entities;

namespace ECSTest.Components
{
    public struct ProjectileFlyComponent : IComponentData
    {
        public float LastEmitDistance;
    }
}