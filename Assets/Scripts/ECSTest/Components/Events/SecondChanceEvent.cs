using Unity.Entities;

namespace ECSTest.Components
{
    public struct SecondChanceEvent : IComponentData, IEnableableComponent
    {
        public int CountPowerCells;
        public float Range;
    }
}