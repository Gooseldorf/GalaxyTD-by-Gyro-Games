using Unity.Mathematics;
using UnityEngine;

[System.Serializable]
public class SquareMapObstacle : ISquareObstacle
{
    [field: SerializeField]
    public float2[] Points { get; private set; }
    [field: SerializeField]
    public AllEnums.ObstacleType ObstacleType { get; private set; }

    public SquareMapObstacle(float2[] points, AllEnums.ObstacleType obstacleType)
    {
        Points = points;
        ObstacleType = obstacleType;
    }
}
