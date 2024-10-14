using Unity.Entities;

namespace ECSTest.Components
{
    public struct Movable : IComponentData
    {
        public float MoveSpeedModifer;
        public bool IsGoingIn;
    }
}