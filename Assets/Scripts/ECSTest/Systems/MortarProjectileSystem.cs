using ECSTest.Components;
using ECSTest.Systems;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(MovingSystem))]
public partial struct MortarProjectileSystem : ISystem
{
    [BurstCompile(CompileSynchronously = true)]
    void OnUpdate(ref SystemState state)
    {
        var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();

        new FlyMortarProjectileJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            EntityCommandBuffer = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
        }
        .ScheduleParallel();
    }

    [BurstCompile(CompileSynchronously = true)]
    private partial struct FlyMortarProjectileJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;

        private void Execute(ref MortarProjectile mortarProjectile, [ChunkIndexInQuery] int chunkIndex, [EntityIndexInChunk] int indexInChunk, Entity mortarEntity,
            ref DestroyComponent destroyComponent)
        {
            if(destroyComponent.IsNeedToDestroy)
                return;
            
            mortarProjectile.RemainingTime -= DeltaTime;
            if (mortarProjectile.RemainingTime > 0) return;
            
            int sortKey = chunkIndex;
            CreateCollision(EntityCommandBuffer, sortKey, mortarProjectile);
            destroyComponent.IsNeedToDestroy = true;
            destroyComponent.DestroyDelay = 2;
        }

        private void CreateCollision(EntityCommandBuffer.ParallelWriter entityCommandBuffer, int sortKey, MortarProjectile mortarProjectile)
        {
            Entity collisionEntity = entityCommandBuffer.CreateEntity(sortKey);
            entityCommandBuffer.SetName(sortKey, collisionEntity, $"{nameof(AOECollisionEvent)}");
            AOECollisionEvent collisionEvent = new(mortarProjectile);
            entityCommandBuffer.AddComponent(sortKey, collisionEntity, collisionEvent);
        }
    }
}