using ECSTest.Components;
using Unity.Entities;

namespace ECSTest.Aspects
{
    public readonly partial struct CreepPositionAspect : IAspect

    {
        public readonly Entity Entity;
        public readonly RefRO<CreepComponent> Creep;
        public readonly RefRO<PositionComponent> Position;
        public readonly RefRO<RoundObstacle> Obstacle;
        public readonly RefRO<Movable> Movable;
    }
}