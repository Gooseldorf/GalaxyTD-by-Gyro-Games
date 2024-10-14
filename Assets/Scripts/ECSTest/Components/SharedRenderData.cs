using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace ECSTest.Components
{
    public struct SharedRenderData : ISharedComponentData
    {
        public float3 DeathColor;

        public float Scale;
        public float HpBarOffset;
        public float HpBarWidth;
        public float TimeBetweenRunFrames;
        public float TimeBetweenDieFrames;

        public AllEnums.CreepType CreepType;

        public byte RunFrames;
        public byte DieFrames;
    }
}
