using Unity.Entities;

namespace ECSTest.Components
{
    public struct IntBuffer : IBufferElementData
    {
        public int Value;
    }
}