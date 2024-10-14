using ECSTest.Components;
using Unity.Entities;

namespace ECSTest.Aspects
{
    public readonly partial struct CreepAnimationAspect : IAspect
    {
        public readonly RefRW<AnimationComponent> AnimationComponent;
        public readonly RefRO<PositionComponent> PositionComponent;
        public readonly RefRO<RadiationComponent> RadiationComponent;
        public readonly RefRO<CreepComponent> CreepComponent;
        public readonly RefRO<SlowComponent> SlowComponent;
        public readonly RefRO<StunComponent> StunComponent;
        public readonly RefRO<FearComponent> FearComponent;
    }
}
