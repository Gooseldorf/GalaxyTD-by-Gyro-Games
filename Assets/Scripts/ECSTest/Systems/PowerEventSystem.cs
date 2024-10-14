using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public partial struct PowerEventSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (powerEvent, timer, buffer) in SystemAPI.Query<RefRW<PowerEvent>, RefRW<Timer>, DynamicBuffer<PowerEventBuffer>>())
        {
            if (timer.ValueRO.Activated)
            {
                timer.ValueRW.Activated = false;
                powerEvent.ValueRW.IsActive = true;
            }

            if (powerEvent.ValueRW.IsActive)
            {
                powerEvent.ValueRW.IsActive = false;
                //Activating Timers
                foreach (var powerEventBuffer in buffer)
                {
                    Timer powerSwitchTimer = state.EntityManager.GetComponentData<Timer>(powerEventBuffer.Entity);
                    powerSwitchTimer.Activations = powerEventBuffer.Activations;
                    powerSwitchTimer.Value = powerEventBuffer.Timer;
                    state.EntityManager.SetComponentData(powerEventBuffer.Entity, powerSwitchTimer);
                }
            }
        }
    }

}
