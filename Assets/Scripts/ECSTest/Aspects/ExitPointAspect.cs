using ECSTest.Components;
using Unity.Entities;

namespace ECSTest.Aspects
{
    public readonly partial struct ExitPointAspect : IAspect
    {
        public readonly RefRO<GridPositionComponent> GridPosition;
        public readonly RefRO<PositionComponent> PositionComponent;
        private readonly RefRO<ExitPointComponent> exitPointComponent;
    }
}