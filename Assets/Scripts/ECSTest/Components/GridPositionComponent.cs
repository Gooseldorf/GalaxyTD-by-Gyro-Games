using ECSTest.Structs;
using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct GridPositionComponent : IComponentData
    {
        public GridPositionStruct Value;

        public GridPositionComponent(int2 gridPos, int2 gridSize)
        {
            Value = new GridPositionStruct(gridPos, gridSize);
        }
    }
}