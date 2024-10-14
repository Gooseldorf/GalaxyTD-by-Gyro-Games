using Unity.Entities;

namespace ECSTest.Components
{
    public struct DropZoneComponent : IComponentData
    {
        public bool IsOccupied;
        public bool IsPossibleToBuild;
        public bool IsCanInfluenceToFlowField;
        public float TimeToReactivate;
    }
}