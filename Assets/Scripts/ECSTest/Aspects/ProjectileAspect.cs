using ECSTest.Components;
using Unity.Entities;

namespace ECSTest.Aspects
{
    public readonly partial struct ProjectileAspect: IAspect
    {
        public readonly RefRW<ProjectileComponent> ProjectileComponent;
        public readonly RefRW<PositionComponent> PositionComponent;
        public readonly RefRW<DestroyComponent> DestroyComponent;
    }
}