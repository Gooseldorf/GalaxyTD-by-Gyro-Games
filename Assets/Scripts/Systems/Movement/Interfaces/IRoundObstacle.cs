using Unity.Mathematics;

public interface IRoundObstacle : IObstacle
{
    float Range { get; }
    float2 Center { get; }
}