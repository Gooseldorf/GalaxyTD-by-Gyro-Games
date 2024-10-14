using Unity.Entities;

namespace ECSTest.Components
{
    public struct TeleportationMovable : IComponentData
    {
        public float MaxTime;
        public float JumpTime;
        public int MinCountJumps;
        public int MaxCountJumps;
    }
}