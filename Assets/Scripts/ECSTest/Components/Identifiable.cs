using Unity.Entities;

namespace ECSTest.Components
{
    public struct Identifiable : IComponentData
    {
        public int Id;
    }
}