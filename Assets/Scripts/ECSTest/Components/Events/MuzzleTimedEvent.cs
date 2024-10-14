using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Serialization;
using static AllEnums;

namespace ECSTest.Components
{
    public struct MuzzleTimedEvent : IComponentData, IEnableableComponent
    {
        [FormerlySerializedAs("Id")] public TowerId TowerId;
        public Entity Tower;
        public float2 Direction;
        public float2 Position;
        public float AnimationTimer;
        public int CurrentFrame;
        public bool IsEnhanced;
    }
    //TODO: mb combine with muzzle?
    public struct ImpactTimedEvent : IComponentData
    {
        [FormerlySerializedAs("Id")] public TowerId TowerId;
        public float2 Direction;
        public float2 Position;
        public float AnimationTimer;
        public float AoeScale;
        public int CurrentFrame;
        public bool IsEnhanced;
    }
}