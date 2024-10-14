using Unity.Entities;
using Unity.Mathematics;
using static AllEnums;

namespace ECSTest.Components
{
    public struct PowerCellEvent : IComponentData
    {
        public CellEventType EventType;
        public Entity Core;
        public int Value;
        public float2 Position;
    }
}
