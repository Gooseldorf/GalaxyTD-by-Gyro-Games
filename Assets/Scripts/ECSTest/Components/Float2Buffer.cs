using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct Float2Buffer : IBufferElementData
    {
        private float2 value;

        public static implicit operator float2(Float2Buffer e)
        {
            return e.value;
        }

        public static implicit operator Float2Buffer(float2 e)
        {
            return new Float2Buffer {value = e};
        }
    }
}