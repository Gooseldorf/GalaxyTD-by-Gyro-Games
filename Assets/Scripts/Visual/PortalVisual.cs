using ECSTest.Components;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using static MusicManager;

public class PortalVisual : EnvironmentVisual, IPowerableVisual
{
    private readonly int2 portalOffset = new int2(-1, -1);
    [SerializeField] private EnvironmentVisual inPortal;
    [SerializeField] private EnvironmentVisual outPortal;

    [ShowInInspector] public int Id { get; set; }
    [field: SerializeField] public bool IsPowered { get; set; }
    public EnvironmentVisual OutPortal => outPortal;
    public EnvironmentVisual InPortal => inPortal;

    [ShowInInspector, ReadOnly] private Portal portal;

    [Button]
    public void TogglePower() => SetPowered(!IsPowered);

    public void SetPowered(bool isPowered)
    {
        IsPowered = isPowered;

        InPortal.Icon.enabled = IsPowered;
        OutPortal.Icon.enabled = IsPowered;

        if (IsPowered)
        {
            TryPlaySound(InPortal.transform);
            TryPlaySound(OutPortal.transform);
        }
        else
        {
            StopSound3D(sound, InPortal.transform);
            StopSound3D(sound, OutPortal.transform);
        }
    }

    public void InitVisual(PortalComponent portalComponent, PowerableComponent powerable)
    {
        InPortal.InitPosition(new GridPosition(portalComponent.In.GridPos, portalComponent.In.GridSize));
        OutPortal.InitPosition(new GridPosition(portalComponent.Out.GridPos, portalComponent.Out.GridSize));

        InPortal.Icon.enabled = powerable.IsTurnedOn;
        OutPortal.Icon.enabled = powerable.IsTurnedOn;
    }

    public override void InitVisual(object data)
    {
        Portal portal = data as Portal;

        this.portal = portal;
        InPortal.InitPosition(portal.In);
        OutPortal.InitPosition(portal.Out);
    }

    public Portal GetPortalData(int2 gridPositionOffset)
    {
        Portal portal = new Portal()
        {
            In = new GridPosition(InPortal.GridPosition + gridPositionOffset + portalOffset, InPortal.GridSize),
            Out = new GridPosition(OutPortal.GridPosition + gridPositionOffset + portalOffset, OutPortal.GridSize),
            IsPowered = IsPowered
        };
        return portal;
    }
}