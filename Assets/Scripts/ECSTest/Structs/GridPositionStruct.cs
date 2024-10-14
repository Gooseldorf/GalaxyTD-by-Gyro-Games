using Unity.Mathematics;

namespace ECSTest.Structs
{
    public struct GridPositionStruct
    {
        public int2 GridPos;
        public int2 GridSize;
        
        public GridPositionStruct(int2 gridPos, int2 gridSize)
        {
            GridPos = gridPos;
            GridSize = gridSize;
        }

        public int Area => GridSize.x * GridSize.y;
    }
}