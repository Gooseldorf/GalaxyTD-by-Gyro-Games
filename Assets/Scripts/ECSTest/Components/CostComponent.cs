using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct CostComponent : IComponentData
    {
        public int Cost;
        public float SellModifier;
        public float CostMultiplier;

        public int SellCost => (int)math.round(Cost * SellModifier);
    }
}