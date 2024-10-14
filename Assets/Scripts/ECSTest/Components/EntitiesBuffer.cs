using Unity.Entities;

namespace ECSTest.Components
{
    public struct EntitiesBuffer : IBufferElementData
    {
        private Entity entity;

        public static implicit operator Entity(EntitiesBuffer e)
        {
            return e.entity;
        }

        public static implicit operator EntitiesBuffer(Entity e)
        {
            return new EntitiesBuffer {entity = e};
        }
    }
}