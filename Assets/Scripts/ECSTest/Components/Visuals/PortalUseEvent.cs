using ECSTest.Components;
using Unity.Entities;
using Unity.Mathematics;

public struct PortalUseEvent : IComponentData
{
    /// <summary>
    /// Play In Effect of teleportation here(Creep Effect)
    /// </summary>
    public float2 InPosition;
    /// <summary>
    /// Play Out Effect of teleportation here(Creep Effect)
    /// </summary>
    public float2 OutPosition;

    /// <summary>
    /// Play Portal Activation Effect On Portal.In && Portal.Out
    /// </summary>
    public PortalComponent Portal; 

}
