using Unity.Entities;

namespace ECSTest.Components.Singletons
{
    public struct CashUpdateComponent : IComponentData
    {
        public float Variation;
    }
}