using ECSTest.Components;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ECSTest.Systems
{
    [UpdateAfter(typeof(MovingSystem))]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct DebuffSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new CalculateDebuffTimers()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel();
        }

        [BurstCompile]
        private partial struct CalculateDebuffTimers : IJobEntity
        {
            [ReadOnly] public float DeltaTime;

            public void Execute(ref SlowComponent slowComponent, ref StunComponent stunComponent, ref FearComponent fearComponent,in DestroyComponent destroyComponent)
            {
                if(destroyComponent.IsNeedToDestroy)
                    return;
                
                if (slowComponent.Time > 0) slowComponent.Time -= DeltaTime;
                if (slowComponent.Time <= 0)
                {
                    slowComponent.Percent = 0;
                    slowComponent.Time = 0;
                }

                if (stunComponent.Time > 0) stunComponent.Time -= DeltaTime;

                if (fearComponent.Time > 0) fearComponent.Time -= DeltaTime;
            }
        }
    }
}
