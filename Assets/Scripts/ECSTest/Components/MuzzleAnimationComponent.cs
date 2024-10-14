using Unity.Entities;

namespace ECSTest.Components
{
    public struct MuzzleAnimationComponent : IComponentData
    {
        public AllEnums.TowerId TowerType;
        //public bool IsEnhunced;
        public float AnimationTimer;        
        public byte MaxFrameNumber;
        public byte CurrentFrameNumber;

        //public float4 Color;
    }
}
