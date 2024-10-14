using Unity.Mathematics;

public interface IGridPosition : IPosition
{
    float3 IPosition.Position
    {
        get => GetCenterPosition(this);
        set { }
    }

    int2 GridPos { get; }
    int2 GridSize { get; }
    
    public static float3 GetCenterPosition(IGridPosition gridPosition) =>
        GetCenterPosition(gridPosition.GridPos, gridPosition.GridSize);

    public static float3 GetCenterPosition(int2 gridPos, int2 gridSize) =>
        new float3(gridPos + (float2)gridSize / 2, 0);
}