using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct RoundObstacle : IComponentData
    {
        public float Range;
        public AllEnums.ObstacleType ObstacleType;
    }   
}