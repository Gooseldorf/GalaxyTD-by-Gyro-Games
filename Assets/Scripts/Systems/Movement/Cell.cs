using ECSTest.Systems;
using System;

public struct Cell
{
    public float BaseCost;
    public float IntegrationCost;
    public float DiscomfortCost;

    public float MoveSpeedModifier;
    
    public bool IsWall => BaseCost == FlowFieldBuildCacheSystem.LockCost;
    public bool IsCanWalk;
    
    public void SetDefaultCost()
    {
        BaseCost = FlowFieldBuildCacheSystem.BaseCost / MoveSpeedModifier;
    }

    public void SetLockCost()
    {
        BaseCost = FlowFieldBuildCacheSystem.LockCost;
    }

    public bool Equals(Cell other)
    {
        return IntegrationCost.Equals(other.IntegrationCost) && BaseCost.Equals(other.BaseCost) && MoveSpeedModifier.Equals(other.MoveSpeedModifier);
    }

    public override bool Equals(object obj)
    {
        return obj is Cell other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IntegrationCost, BaseCost, MoveSpeedModifier);
    }
}