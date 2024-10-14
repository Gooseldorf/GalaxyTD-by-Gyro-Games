using ECSTest.Components;
using ECSTest.Systems;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(MovingSystem))]
public partial struct RocketProjectileSystem : ISystem
{
    [BurstCompile(CompileSynchronously = true)]
    void OnUpdate(ref SystemState state)
    {
        var singleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();

        new FlyRocketsJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            EntityCommandBuffer = singleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
        }.ScheduleParallel();
    }

    [BurstCompile(CompileSynchronously = true)]
    private partial struct FlyRocketsJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;

        void Execute(ref RocketProjectile rocketFlyData, ref PositionComponent position, ref DestroyComponent destroyComponent, [ChunkIndexInQuery] int chunkIndex, [EntityIndexInChunk] int indexInChunk, Entity rocket)
        {
            if(destroyComponent.IsNeedToDestroy)
                return;
            
            float pathProgressChange = DeltaTime / rocketFlyData.TotalFlyTime;
            rocketFlyData.PathProgress += pathProgressChange;
            if (rocketFlyData.PathProgress >= 1)
            {
                int sortKey = chunkIndex;
                CreateCollision(EntityCommandBuffer, sortKey, rocketFlyData);
                destroyComponent.IsNeedToDestroy = true;
                destroyComponent.DestroyDelay = 2;
                EntityCommandBuffer.SetComponentEnabled<RocketProjectile>(sortKey, rocket, false);
            }
            else
            {
                float2 nextPos = rocketFlyData.GetPosition();
                position.Direction = math.normalize(nextPos - position.Position);
                position.Position = nextPos;
            }
        }

        private void CreateCollision(EntityCommandBuffer.ParallelWriter entityCommandBuffer, int sortKey, RocketProjectile rocketProjectile)
        {
            Entity collisionEntity = entityCommandBuffer.CreateEntity(sortKey);
            entityCommandBuffer.SetName(sortKey, collisionEntity, $"{nameof(AOECollisionEvent)}");
            AOECollisionEvent collisionEvent = new(rocketProjectile);
            entityCommandBuffer.AddComponent(sortKey, collisionEntity, collisionEvent);
        }
    }


}