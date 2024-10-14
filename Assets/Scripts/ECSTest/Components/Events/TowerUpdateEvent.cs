using Unity.Entities;

namespace ECSTest.Components
{
    public struct TowerUpdateEvent : IComponentData
    {
        public Entity TowerEntity;
        public bool IsTurnedOn;
    }
}