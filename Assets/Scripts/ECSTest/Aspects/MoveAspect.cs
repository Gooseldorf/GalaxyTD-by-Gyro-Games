using ECSTest.Components;
using Unity.Entities;

namespace ECSTest.Aspects
{
    public readonly partial struct MoveAspect : IAspect
    {
        public readonly RefRW<PositionComponent> PositionComponent;
        public readonly RefRO<CreepComponent> CreepComponent;
        public readonly RefRO<Movable> MovableComponent;
        // public readonly RefRO<RoundObstacle> Obstacle;
        public readonly RefRW<Knockback> Knockback;
        public readonly RefRO<SlowComponent> SlowComponent;
        public readonly RefRO<StunComponent> StunComponent;
        public readonly RefRO<FearComponent> FearComponent;
        public readonly RefRO<DestroyComponent> DestroyComponent;
    }
}