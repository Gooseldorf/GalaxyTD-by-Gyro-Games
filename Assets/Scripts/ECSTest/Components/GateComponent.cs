using ECSTest.Structs;
using Unity.Entities;

public struct GateComponent : IComponentData
{
    public GridPositionStruct StartPosition;
    public GridPositionStruct EndPosition;
    public GridPositionStruct MiddlePosition;
}
