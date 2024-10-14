using System;
using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct AnimatedTextComponent : IComponentData, IComparable<AnimatedTextComponent>
    {
        public float4 Color;
        public float2 Position;
        public float Timer;
        public float Scale;
        public AllEnums.TextType TextType;
        public int NonCashValue;
        public int CashValue;//try to remove later

        // TextVisualizationSystem use this only for textComponents.Sort()
        public int CompareTo(AnimatedTextComponent other)
        {

            if (CashValue != 0 && other.CashValue == 0) return 1;
            if (CashValue == 0 && other.CashValue != 0) return -1;
            if (CashValue != 0 && other.CashValue != 0) return CashValue.CompareTo(other.CashValue);
            return NonCashValue.CompareTo(other.NonCashValue);
        }
    }
}