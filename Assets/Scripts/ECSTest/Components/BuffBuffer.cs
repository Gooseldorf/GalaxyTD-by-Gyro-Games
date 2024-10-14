using Unity.Entities;

namespace ECSTest.Components
{
    public struct BuffBuffer : IBufferElementData
    {
        public int Type;
        public float Timer;
        public float BuffValue;
    }
}