using Unity.Entities;

namespace ECSTest.Components
{
    public struct RelicComponent : IComponentData
    {
        public Entity host;
    }
}