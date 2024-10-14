using Unity.Entities;

namespace ECSTest.Components
{
    public struct EnergyCoreComponent : IComponentData
    {
        public int PowerCellCount;
        public bool IsTurnedOn;
        public float DeactivationTime;
        public float TurnedOffTime;
    }
}