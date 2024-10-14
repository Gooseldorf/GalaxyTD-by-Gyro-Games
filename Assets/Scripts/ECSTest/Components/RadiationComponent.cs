using Unity.Entities;

namespace ECSTest.Components
{
    public struct RadiationComponent : IComponentData
    {
        public float Time;
        public float DPS;
    }
}
