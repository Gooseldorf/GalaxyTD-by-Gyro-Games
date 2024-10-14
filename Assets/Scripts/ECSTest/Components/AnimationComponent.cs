using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct AnimationComponent : IComponentData
    {
        public float4 Color;
        public float2 Direction;
        public float AnimationTimer;
        public float DamageTimer;
        public bool IsOutline;
        public bool DamageTaken;
        public byte FrameNumber;
        public AllEnums.AnimationState AnimationState;
    }
}
