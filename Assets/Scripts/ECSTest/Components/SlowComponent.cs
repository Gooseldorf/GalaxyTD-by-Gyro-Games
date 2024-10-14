using Unity.Entities;

namespace ECSTest.Components
{
    public struct SlowComponent : IComponentData
    {  
        public float Time;
        public float Percent;
    }
}
