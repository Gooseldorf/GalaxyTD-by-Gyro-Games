using Unity.Entities;

namespace ECSTest.Components
{
    public struct EntityHolderComponent : IComponentData
    {
        public Entity Entity;

        public static implicit operator EntityHolderComponent(Entity entity) => new() { Entity = entity };
        public static implicit operator Entity(EntityHolderComponent holder) => holder.Entity;
    }
}