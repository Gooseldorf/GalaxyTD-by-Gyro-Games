using ECSTest.Components;
using ECSTest.Systems;
using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(FlowFieldBuildCacheSystem))]
public partial struct PowerSystemNew : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        BeginFixedStepSimulationEntityCommandBufferSystem.Singleton singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer buffer = singleton.CreateCommandBuffer(state.WorldUnmanaged);

        EntityManager entityManager = state.EntityManager;
        foreach (var (powerSwitch, timer, connections, entity) in SystemAPI.Query<RefRW<PowerSwitch>, RefRW<Timer>, DynamicBuffer<EntitiesBuffer>>().WithEntityAccess())
        {
            if (timer.ValueRO.Activated)
            {
                timer.ValueRW.Activated = false;
                powerSwitch.ValueRW.IsTurnedOn = !powerSwitch.ValueRO.IsTurnedOn;

                foreach (Entity connectedPowerable in connections)
                {
                    PowerableComponent powerable = entityManager.GetComponentData<PowerableComponent>(connectedPowerable);
                    powerable.IsPowered = powerSwitch.ValueRO.IsTurnedOn;
                    entityManager.SetComponentData(connectedPowerable, powerable);
                    Entity powerEvent = buffer.CreateEntity();
                    buffer.SetName(powerEvent, nameof(ChangePowerEvent));
                    buffer.AddComponent(powerEvent, new ChangePowerEvent() {Entity = connectedPowerable, IsTurnedOn = powerable.IsTurnedOn});
                }

                Entity baseCostEvent = buffer.CreateEntity();
                buffer.SetName(baseCostEvent, nameof(BaseCostChangedEvent));
                buffer.AddComponent(baseCostEvent, new BaseCostChangedEvent());
            }
        }
    }
}