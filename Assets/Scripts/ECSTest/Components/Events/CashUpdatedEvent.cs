using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct CashUpdatedEvent : IComponentData
    {
        public int CashAmount;
        public float2 Position;
        public bool CashForWave;
    }
}
