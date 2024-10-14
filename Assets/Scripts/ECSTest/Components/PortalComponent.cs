using ECSTest.Structs;
using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct PortalComponent : IComponentData
    {
        public GridPositionStruct In;
        public GridPositionStruct Out;

        public PortalComponent(Portal portalData)
        {
            In = new GridPositionStruct(portalData.In.GridPos, portalData.In.GridSize);
            Out = new GridPositionStruct(portalData.Out.GridPos, portalData.Out.GridSize);
        }

        public float2 RandomOutPosition(ref Random random, float2 offset)
        {
            return Out.GridPos + offset + random.NextFloat2(Out.GridSize - 2 * offset);
        }
    }
}