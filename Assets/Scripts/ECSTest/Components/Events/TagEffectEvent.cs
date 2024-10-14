using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct TagEffectEvent : IComponentData
    {
        public AllEnums.TagEffectType EffectType;
        public float AoeRange;
        public float2 Point;
    }
}