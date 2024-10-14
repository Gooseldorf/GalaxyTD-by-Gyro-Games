using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct ConveyorComponent: IComponentData
    {
        public int2 Direction;
        public float Speed;

        public ConveyorComponent(Conveyor data)
        {
            Direction = data.ConveyorDirection;
            Speed = data.Speed;
        }
    }
}