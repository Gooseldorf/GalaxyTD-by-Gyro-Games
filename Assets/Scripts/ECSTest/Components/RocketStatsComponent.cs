using Unity.Entities;
using Unity.Mathematics;

public struct RocketStatsComponent : IComponentData
{
    public float AOE;
    //Mortars Have 0 Scatter
    public float ScatterDistance;
    public float2 LastTargetPosition;
    public float2 LastTargetVelocity;

    public RocketStatsComponent(RocketStats rocketStats) : this()
    {
        AOE = rocketStats.AOE;
        ScatterDistance = rocketStats.ScatterDistance;
        LastTargetPosition = default;
        LastTargetVelocity = default;
    }
}
