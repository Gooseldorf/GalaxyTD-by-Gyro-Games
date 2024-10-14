using ECSTest.Components;
using Unity.Burst;
using Unity.Entities;

namespace ECSTest.Systems
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileSystemBase))]
    public partial struct DestroySystemBase : ISystem
    {
        [BurstCompile(CompileSynchronously = true)]
        public void OnUpdate(ref SystemState state)
        {
            var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();

                new CleanUpJob() {EntityCommandBuffer = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(), DeltaTime = SystemAPI.Time.DeltaTime}.ScheduleParallel();
        }

        [BurstCompile(CompileSynchronously = true)]
        private partial struct CleanUpJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;
            public float DeltaTime;

            public void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex, ref DestroyComponent destroyComponent)
            {
                if (!destroyComponent.IsNeedToDestroy) return;

                destroyComponent.DestroyDelay -= DeltaTime;
                
                if (destroyComponent.DestroyDelay > 0) return;
                
                EntityCommandBuffer.DestroyEntity(chunkIndex, entity);
            }
        }
    }
}