using Unity.Mathematics;

namespace ECSTest.Components
{
    [System.Serializable]
    public struct ShootVfxAnimationFrameData
    {
        public float4 UV;
        public float2 Scale;
        public float2 PositionOffset;
        public float ScaleModifier;
    }
}
