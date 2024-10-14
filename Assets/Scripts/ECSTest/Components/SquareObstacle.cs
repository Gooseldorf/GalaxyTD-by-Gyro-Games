using ECSTest.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct SquareObstacle : IComponentData,IEnableableComponent
    {
        public float2 BotLeftPoint;
        public float2 TopLeftPoint;
        public float2 TopRightPoint;
        public float2 BotRightPoint;

        public AllEnums.ObstacleType ObstacleType;
    }


    public static class SquareObstacleUtils
    {
        public static NativeArray<float2> GetPoints(this SquareObstacle squareObstacle)
        {
            NativeArray<float2> points = new(4, Allocator.Temp);
            points[0] = squareObstacle.BotLeftPoint;
            points[1] = squareObstacle.TopLeftPoint;
            points[2] = squareObstacle.TopRightPoint;
            points[3] = squareObstacle.BotRightPoint;
            return points;
        }

        public static NativeArray<float2> GetPoints(this ObstacleInfo obstacle)
        {
            NativeArray<float2> points = new(4, Allocator.Temp);
            points[0] = obstacle.BotLeftPoint;
            points[1] = obstacle.TopLeftPoint;
            points[2] = obstacle.TopRightPoint;
            points[3] = obstacle.BotRightPoint;
            return points;
        }
    }
}