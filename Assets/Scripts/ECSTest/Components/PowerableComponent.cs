using Unity.Entities;

namespace ECSTest.Components
{
    public struct PowerableComponent : IComponentData
    {
        public bool IsPowered;
        public bool Reversed;


        public bool IsTurnedOn => Reversed ? !IsPowered : IsPowered;

        public PowerableComponent(bool isTurnedOnByDefault = true)
        {
            IsPowered = true;
            Reversed = !isTurnedOnByDefault;
        }
        public PowerableComponent(bool isPowered, bool reversed)
        {
            IsPowered = isPowered;
            Reversed = reversed;
        }
    }
}