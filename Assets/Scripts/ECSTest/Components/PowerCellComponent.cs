using Unity.Entities;

namespace ECSTest.Components
{
    public struct PowerCellComponent : IComponentData
    {
        public Entity Creep;
        public Entity CurrentCore;
        public Entity SaveCore;
        public bool IsMoves;
        
        public void AttachToCreep(Entity creepEntity)
        {
            Creep = creepEntity;
            IsMoves = true;
        }

        public void Detach()
        {
            Creep = Entity.Null;
        }

        public void ReturnToCore()
        {
            IsMoves = false;
        }
    }
} 