using Unity.Mathematics;
using UnityEngine;

public class ExitPointVisual : EnvironmentVisual
{
    [SerializeField] private int2 exitPointOffset;
    [SerializeField] private Sprite combinedZoneSprite;

    private bool isCombinedZone;

    public void SetCombinedZoneSprite()
    {
        icon.sprite = combinedZoneSprite;
        isCombinedZone = true;
    }

    public void InitExitPointVisual(float2 position, bool isCombined)
    {
        transform.position = new float3(position, 0);
        if (isCombined)
            SetCombinedZoneSprite();
    }

    public ExitPoint GetExitPointData(int2 gridPositionOffset)
    {
        return new ExitPoint(GridPosition + gridPositionOffset + exitPointOffset, GridSize, isCombinedZone);
    }

    public override void InitVisual(object data)
    {
        ExitPoint exitPoint = data as ExitPoint;
        
        //Id = exitPoint.Id;
        InitPosition(exitPoint);
        if (exitPoint.IsCombinedZone)
        {
            SetCombinedZoneSprite();
        }
    }
}
