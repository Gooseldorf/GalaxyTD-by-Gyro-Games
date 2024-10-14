using Unity.Mathematics;

public interface ISquareObstacle : IObstacle
{
    float2[] Points { get; }
}