using ECSTest.Components;
using Unity.Entities;

namespace ECSTest.Aspects
{
    public readonly partial struct PowerCellAspect : IAspect
    {
        public readonly RefRW<TimerComponent> Timer;
        public readonly RefRW<PowerCellComponent> PowerCellComponent;
        public readonly RefRW<PositionComponent> Position;
        public readonly RefRO<DestroyComponent> DestroyComponent;
        public readonly Entity Entity;

        public void AttachToCreep(Entity creepEntity)
        {
            PowerCellComponent.ValueRW.AttachToCreep(creepEntity);
        }

        public void Detach()
        {
            PowerCellComponent.ValueRW.Detach();
        }

        public void ReturnToCore()
        {
            PowerCellComponent.ValueRW.ReturnToCore();
        }
    }
}