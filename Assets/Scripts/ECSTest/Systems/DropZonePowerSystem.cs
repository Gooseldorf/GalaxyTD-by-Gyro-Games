using ECSTest.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using static AllEnums;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PowerSystemNew))]
    public partial struct DropZonePowerSystem : ISystem
    {
        private EntityQuery changedPowerEventQuery;

        public void OnCreate(ref SystemState state)
        {
            changedPowerEventQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<ChangePowerEvent>().Build(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (changedPowerEventQuery.IsEmpty)
                return;

            EntityManager manager = state.EntityManager;

            NativeArray<ChangePowerEvent> powerEvents = changedPowerEventQuery.ToComponentDataArray<ChangePowerEvent>(Allocator.Temp);

            foreach (ChangePowerEvent powerEvent in powerEvents)
            {
                if (manager.HasComponent<DropZoneComponent>(powerEvent.Entity))
                {
                    EntityHolderComponent towerEntity = manager.GetComponentData<EntityHolderComponent>(powerEvent.Entity);

                    if (towerEntity == Entity.Null)
                        continue;

                    PowerableComponent powerableComponent = manager.GetComponentData<PowerableComponent>(towerEntity);

                    powerableComponent.IsPowered = powerEvent.IsTurnedOn;
                    manager.SetComponentData(towerEntity, powerableComponent);

                    var towerUpdateEntity = manager.CreateEntity();
                    manager.SetName(towerUpdateEntity, nameof(TowerUpdateEvent));
                    manager.AddComponentData(towerUpdateEntity, new TowerUpdateEvent {TowerEntity = towerEntity, IsTurnedOn = powerableComponent.IsTurnedOn});
                }
            }

            powerEvents.Dispose();
        }
    }
}