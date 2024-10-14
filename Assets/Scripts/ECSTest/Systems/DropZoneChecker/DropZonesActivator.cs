using ECSTest.Components;
using System.ComponentModel;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(FlowFieldBuildCacheSystem))]
    public partial struct DropZonesActivator : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();

            new DropZonesActivatorJob()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Ecb = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();

        }

        [BurstCompile]
        public partial struct DropZonesActivatorJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;
            public void Execute([ChunkIndexInQuery] int chunkIndex, [EntityIndexInChunk] int indexInChunk, ref DropZoneComponent dropZoneComponent, Entity entity)
            {
                if (dropZoneComponent.IsOccupied && dropZoneComponent.TimeToReactivate > 0)
                {
                    dropZoneComponent.TimeToReactivate -= DeltaTime;
                    if (dropZoneComponent.TimeToReactivate <= 0)
                    {
                        dropZoneComponent.IsOccupied = false;
                        dropZoneComponent.TimeToReactivate = 0;

                        int key = (chunkIndex * 128 + indexInChunk);
                        Entity dropZoneEvent = Ecb.CreateEntity(key);
                        Ecb.SetName(key, dropZoneEvent, nameof(DropZoneEvent));
                        Ecb.AddComponent(key, dropZoneEvent, new DropZoneEvent() { Entity = entity });
                    }
                }
            }
        }
    }
}