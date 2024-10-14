using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;
using ECSTest.Systems;
using ECSTest.Components;

public partial struct TimerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TimeSkipper>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        TimeSkipper timeSkipper = SystemAPI.GetSingleton<TimeSkipper>();
        if (timeSkipper.CurrentTime((float)SystemAPI.Time.ElapsedTime) < (SpawnerSystem.FirstWaveOffset - SpawnerSystem.FirstWaveSpawnOffset))
            return;

        foreach (var timer in SystemAPI.Query<RefRW<Timer>>())
        {
            //Timer is used to determine when the power event should be activated
            timer.ValueRW.Value -= SystemAPI.Time.DeltaTime;
            if (timer.ValueRW.Value > 0)
                continue;

            timer.ValueRW.Activations--;
            timer.ValueRW.Activated = true;
            if (timer.ValueRO.Activations <= 0)
                timer.ValueRW.Value = float.PositiveInfinity;
            else
                timer.ValueRW.Value = timer.ValueRO.TimeBetweenToggles;
        }
    }

}
