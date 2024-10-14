using Unity.Entities;
using Unity.Mathematics;

namespace ECSTest.Components
{
    public struct PositionComponent : IComponentData
    {
        public float2 Position;

        /// <summary>
        /// represents Velocity Vector for creeps
        /// </summary>
        public float2 Direction;

        public PositionComponent(float2 position, float2 direction)
        {
            this.Position = position;
            this.Direction = direction;
        }
    }
}